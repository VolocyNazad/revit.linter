using Autodesk.Revit.DB;
using Microsoft.Extensions.Logging.Abstractions;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Services;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Spatial;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.Languages.Factories;
using Revit.Linter.Testing;
using TUnit.Core.Executors;
using CollisionElementDiagnostic = Revit.Linter.CollisionDiagnostics.ElementDiagnostic;

namespace Revit.Linter.Diagnostic.RevitTests;

/// <summary>
/// Covers the collision diagnostic and its spatial index on boxes whose overlaps are known in advance:
/// <c>_center</c> intersects <c>_first</c> and <c>_second</c>, which do not intersect each other,
/// <c>_distant</c> intersects nothing, and <c>_long</c> is far from all of them and too long to be
/// stored in grid cells.
/// </summary>
public sealed class CollisionDiagnosticTests : RevitApiTest
{
    private Document? _document;
    private DirectShape? _center;
    private DirectShape? _first;
    private DirectShape? _second;
    private DirectShape? _distant;
    private DirectShape? _long;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateModel()
    {
        _document = Application.NewProjectDocument(UnitSystem.Metric);

        using Transaction transaction = new(_document, "Seed collision diagnostic tests");
        transaction.Start();
        _center = CreateBox(0, 0, 2, 2);
        _first = CreateBox(1, 1, 2, 2);
        _second = CreateBox(-1.5, -1.5, 2, 2);
        _distant = CreateBox(20, 20, 2, 2);
        _long = CreateBox(-500, 100, 1000, 1);
        transaction.Commit();
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseModel() => _document?.Close(false);

    [Test]
    public async Task Finding_lists_every_intersecting_element()
    {
        CollisionElementDiagnostic diagnostic = CreateDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, _center!);
        ElementId[] dependencyIds =
            [.. feedback.AdditionalTargetDependencies.OfType<Element>().Select(element => element.Id)];
        ElementId[] messageIds = (ElementId[])feedback.AdditionalMessageArguments!["intersection.elementIds"];
        int count = (int)feedback.AdditionalMessageArguments["intersection.count"];
        ElementId[] expectedIds = [_first!.Id, _second!.Id];

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(dependencyIds).IsEquivalentTo(expectedIds);
        await Assert.That(messageIds).IsEquivalentTo(expectedIds);
        await Assert.That(count).IsEqualTo(2);
    }

    [Test]
    public async Task Each_element_of_a_pair_reports_the_other_one()
    {
        CollisionElementDiagnostic diagnostic = CreateDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, _first!);
        ElementId[] dependencyIds =
            [.. feedback.AdditionalTargetDependencies.OfType<Element>().Select(element => element.Id)];
        ElementId[] expectedIds = [_center!.Id];

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(dependencyIds).IsEquivalentTo(expectedIds);
    }

    [Test]
    public async Task Element_without_intersections_is_valid()
    {
        CollisionElementDiagnostic diagnostic = CreateDiagnostic();

        DiagnosticFeedback distantFeedback = diagnostic.Execute(_document!, null, _distant!);
        DiagnosticFeedback longFeedback = diagnostic.Execute(_document!, null, _long!);

        await Assert.That(distantFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(longFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Spatial_index_returns_only_elements_whose_bounds_overlap_the_query()
    {
        BoundingBoxGridIndex index = BuildIndex();

        ElementId[] nearCenter = [.. index.Query(_center!.get_BoundingBox(null)).Select(element => element.Id)];
        ElementId centerId = _center.Id;
        ElementId firstId = _first!.Id;
        ElementId secondId = _second!.Id;
        bool containsDistant = nearCenter.Contains(_distant!.Id);

        await Assert.That(nearCenter).Contains(centerId);
        await Assert.That(nearCenter).Contains(firstId);
        await Assert.That(nearCenter).Contains(secondId);
        await Assert.That(containsDistant).IsFalse();
    }

    [Test]
    public async Task Spatial_index_checks_bounds_of_elements_that_do_not_fit_grid_cells()
    {
        BoundingBoxGridIndex index = BuildIndex();
        BoundingBoxXYZ overLongElement = new()
        {
            Min = new XYZ(300, 99, 0),
            Max = new XYZ(301, 102, 1),
        };

        ElementId longId = _long!.Id;
        bool returnedNearCenter = index.Query(_center!.get_BoundingBox(null))
            .Any(element => element.Id == longId);
        bool returnedNearItself = index.Query(overLongElement)
            .Any(element => element.Id == longId);

        await Assert.That(returnedNearCenter).IsFalse();
        await Assert.That(returnedNearItself).IsTrue();
    }

    [Test]
    public async Task Spatial_index_returns_every_overlapping_element_for_a_query_too_large_for_the_grid()
    {
        BoundingBoxGridIndex index = BuildIndex();
        BoundingBoxXYZ everything = new()
        {
            Min = new XYZ(-10000, -10000, -10),
            Max = new XYZ(10000, 10000, 10),
        };

        int count = index.Query(everything).Count();

        await Assert.That(count).IsEqualTo(5);
    }

    private BoundingBoxGridIndex BuildIndex() => BoundingBoxGridIndex.Build(
        [_center!, _first!, _second!, _distant!, _long!],
        element => element.get_BoundingBox(null));

    private static CollisionElementDiagnostic CreateDiagnostic()
    {
        // A new query service per diagnostic: the test cache never invalidates on transactions.
        IDocumentQueryService queries = TestDocumentQueries.Create();
        FormulaCompilationNotifier notifier = new();
        return new CollisionElementDiagnostic(
            new ElementFilterFactory(NullLogger<ElementFilterFactory>.Instance, notifier),
            new ElementFunctionFactory(NullLogger<ElementFunctionFactory>.Instance, notifier),
            new GetElementBoundingBoxService(queries),
            new GetElementGeometryService(queries),
            queries,
            NullLogger<CollisionElementDiagnostic>.Instance)
        {
            Identity = new ElementDiagnosticId(
                "CLSN-TEST", "Description", "Message", DiagnosticSeverity.Warning, true, false, ""),
            TakeFormula = "builtincategory('OST_GenericModel')",
            GroupByFormula = "property('ViewSpecific')",
        };
    }

    // A direct shape keeps exactly the solid it is given, unlike walls, which Revit joins and trims.
    private DirectShape CreateBox(double minX, double minY, double sizeX, double sizeY)
    {
        XYZ corner0 = new(minX, minY, 0);
        XYZ corner1 = new(minX + sizeX, minY, 0);
        XYZ corner2 = new(minX + sizeX, minY + sizeY, 0);
        XYZ corner3 = new(minX, minY + sizeY, 0);
        List<Curve> edges =
        [
            Line.CreateBound(corner0, corner1),
            Line.CreateBound(corner1, corner2),
            Line.CreateBound(corner2, corner3),
            Line.CreateBound(corner3, corner0),
        ];
        Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(
            new List<CurveLoop> { CurveLoop.Create(edges) }, XYZ.BasisZ, 2);

        DirectShape shape = DirectShape.CreateElement(
            _document!, new ElementId(BuiltInCategory.OST_GenericModel));
        shape.SetShape(new List<GeometryObject> { solid });
        return shape;
    }

    private sealed class FormulaCompilationNotifier : IFormulaCompilationNotifier
    {
        public void Notify()
        {
            // Compilation failures are not part of what these tests assert.
        }
    }
}