using Autodesk.Revit.DB;
#if !IS2021
using Autodesk.Revit.DB.Structure;
#endif
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
/// stored in grid cells. The nested-family test builds a placed family whose components are nested
/// but not shared, and pins the boundary of <c>ignoreNestedFamilies</c>: such components are not
/// project elements and therefore never reach the collision check.
/// </summary>
public sealed class CollisionDiagnosticTests : RevitApiTest
{
    private Document? _document;
    private string? _linkPath;
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
        _center = CreateBox(_document, 0, 0, 2, 2);
        _first = CreateBox(_document, 1, 1, 2, 2);
        _second = CreateBox(_document, -1.5, -1.5, 2, 2);
        _distant = CreateBox(_document, 20, 20, 2, 2);
        _long = CreateBox(_document, -500, 100, 1000, 1);
        transaction.Commit();

        // A linked document with one box overlapping _distant and nothing else. The link instance
        // is placed at the origin, so linked coordinates coincide with host coordinates.
        _linkPath = Path.Combine(Path.GetTempPath(), $"LinkCollisionDiagnostic{Guid.NewGuid():N}.rvt");
        Document linkDocument = Application.NewProjectDocument(UnitSystem.Metric);
        using (Transaction linkTransaction = new(linkDocument, "Seed linked collision box"))
        {
            linkTransaction.Start();
            CreateBox(linkDocument, 20, 20, 2, 2);
            linkTransaction.Commit();
        }
        linkDocument.SaveAs(_linkPath);
        linkDocument.Close(false);

        using (Transaction linkTransaction = new(_document, "Link document for collision tests"))
        {
            linkTransaction.Start();
            LinkLoadResult linkResult = RevitLinkType.Create(
                _document, ModelPathUtils.ConvertUserVisiblePathToModelPath(_linkPath), new RevitLinkOptions(false));
            RevitLinkInstance.Create(_document, linkResult.ElementId);
            linkTransaction.Commit();
        }
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseModel()
    {
        _document?.Close(false);
        if (_linkPath is not null && File.Exists(_linkPath)) File.Delete(_linkPath);
    }

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
    public async Task Linked_collision_is_found_when_enabled()
    {
        // _distant collides with nothing in the host document, so the only hit is the linked box.
        CollisionElementDiagnostic diagnostic = CreateDiagnostic(includeLinks: true);

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, _distant!);
        string elementNames = (string)feedback.AdditionalMessageArguments!["intersection.elementNames"];
        int count = (int)feedback.AdditionalMessageArguments["intersection.count"];

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(count).IsEqualTo(1);
        await Assert.That(elementNames).Contains($"[{Path.GetFileNameWithoutExtension(_linkPath)}]");
        await Assert.That(feedback.AdditionalTargetDependencies).IsEmpty();
    }

    [Test]
    public async Task Linked_collision_is_ignored_when_disabled()
    {
        CollisionElementDiagnostic diagnostic = CreateDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, _distant!);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
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

    // Revit 2021 does not permit creating the test family geometry through the API on the opened
    // templates (the same limitation disables the connector-cube tests there), so the nested-family
    // fixture cannot be built on 2021 and the case stays on the manual checklist for that version.
#if !IS2021
    [Test]
    public async Task Non_shared_nested_components_are_not_project_elements()
    {
        (FamilyInstance parent, DirectShape thirdParty) = CreateNestedFamilyFixture();
        CollisionElementDiagnostic enabled = CreateDiagnostic(ignoreNestedFamilies: true);
        CollisionElementDiagnostic disabled = CreateDiagnostic();

        DiagnosticFeedback enabledFeedback = enabled.Execute(_document!, null, parent);
        DiagnosticFeedback disabledFeedback = disabled.Execute(_document!, null, parent);

        // A nested component that is not shared stays inside the parent family definition: it is
        // not a project element, so it never becomes a collision candidate and the flag cannot
        // change the result. Shared nested families - the only form the flag serves in a project -
        // can be produced only in the family editor, not through the API, so they stay on the
        // manual checklist while this test pins the boundary the check relies on.
        await Assert.That(parent.GetSubComponentIds()).IsEmpty();
        int projectFamilyInstances = new FilteredElementCollector(_document!)
            .OfClass(typeof(FamilyInstance)).GetElementCount();
        await Assert.That(projectFamilyInstances).IsEqualTo(1);
        await Assert.That(DependencyIds(enabledFeedback)).IsEquivalentTo([thirdParty.Id]);
        await Assert.That(DependencyIds(disabledFeedback)).IsEquivalentTo([thirdParty.Id]);
    }

    private static ElementId[] DependencyIds(DiagnosticFeedback feedback) =>
        [.. feedback.AdditionalTargetDependencies.OfType<Element>().Select(element => element.Id)];

    // A placed family with two nested components inside a parent body, and one third-party box
    // crossing the parent. Placed far from the other test elements so this test never depends on
    // them. The family instances are rebuilt per test because the test document is recreated for
    // every test.
    private (FamilyInstance Parent, DirectShape ThirdParty) CreateNestedFamilyFixture()
    {
        Document projectDocument = _document!;
        Document innerDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        Document outerDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            using (Transaction inner = new(innerDocument, "Create nested test cube"))
            {
                inner.Start();
                CreateFamilyCube(innerDocument, 0.5);
                inner.Commit();
            }

            Family innerFamily = innerDocument.LoadFamily(outerDocument);
            FamilySymbol innerSymbol = FindSymbol(outerDocument, innerFamily);
            using (Transaction outer = new(outerDocument, "Create outer test family"))
            {
                outer.Start();
                if (!innerSymbol.IsActive) innerSymbol.Activate();
                CreateFamilyCube(outerDocument, 2);
                // Two components of one parent: they overlap each other and the parent body.
                outerDocument.FamilyCreate.NewFamilyInstance(
                    new XYZ(0, 0, 0.5), innerSymbol, StructuralType.NonStructural);
                outerDocument.FamilyCreate.NewFamilyInstance(
                    new XYZ(0.5, 0.5, 0.5), innerSymbol, StructuralType.NonStructural);
                outerDocument.Regenerate();
                outer.Commit();
            }

            Family family = outerDocument.LoadFamily(projectDocument);

            FamilyInstance parent;
            using (Transaction place = new(projectDocument, "Place nested-family test instance"))
            {
                place.Start();
                FamilySymbol symbol = FindSymbol(projectDocument, family);
                if (!symbol.IsActive) symbol.Activate();
                parent = projectDocument.Create.NewFamilyInstance(
                    new XYZ(200, 200, 0), symbol, StructuralType.NonStructural);
                projectDocument.Regenerate();
                place.Commit();
            }

            DirectShape thirdParty;
            using (Transaction third = new(projectDocument, "Seed nested-family third party"))
            {
                third.Start();
                thirdParty = CreateBox(projectDocument, 200.2, 200.2, 1, 1);
                third.Commit();
            }

            return (parent, thirdParty);
        }
        finally
        {
            innerDocument.Close(false);
            outerDocument.Close(false);
        }
    }

    private static void CreateFamilyCube(Document familyDocument, double halfSize)
    {
        Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
        SketchPlane sketchPlane = SketchPlane.Create(familyDocument, plane);
        CurveArray rectangle = new();
        rectangle.Append(Line.CreateBound(new XYZ(-halfSize, -halfSize, 0), new XYZ(halfSize, -halfSize, 0)));
        rectangle.Append(Line.CreateBound(new XYZ(halfSize, -halfSize, 0), new XYZ(halfSize, halfSize, 0)));
        rectangle.Append(Line.CreateBound(new XYZ(halfSize, halfSize, 0), new XYZ(-halfSize, halfSize, 0)));
        rectangle.Append(Line.CreateBound(new XYZ(-halfSize, halfSize, 0), new XYZ(-halfSize, -halfSize, 0)));
        CurveArrArray profile = new();
        profile.Append(rectangle);
        familyDocument.FamilyCreate.NewExtrusion(true, profile, sketchPlane, halfSize * 2);
    }

    private static FamilySymbol FindSymbol(Document document, Family family) =>
        new FilteredElementCollector(document)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .First(candidate => candidate.Family.Id == family.Id);
#endif

    private BoundingBoxGridIndex BuildIndex() => BoundingBoxGridIndex.Build(
        [_center!, _first!, _second!, _distant!, _long!],
        element => element.get_BoundingBox(null));

    private static CollisionElementDiagnostic CreateDiagnostic(
        bool includeLinks = false, bool ignoreNestedFamilies = false)
    {
        // A new query service per diagnostic: the test cache never invalidates on transactions.
        IDocumentQueryService queries = TestDocumentQueries.Create();
        FormulaCompilationNotifier notifier = new();
        return new CollisionElementDiagnostic(
            new ElementFilterFactory(NullLogger<ElementFilterFactory>.Instance, notifier),
            new ElementFunctionFactory(NullLogger<ElementFunctionFactory>.Instance, notifier),
            new GetElementBoundingBoxService(queries),
            new GetElementGeometryService(queries),
            new GetConnectedElementIdsService(queries),
            queries,
            NullLogger<CollisionElementDiagnostic>.Instance)
        {
            Identity = new ElementDiagnosticId(
                "CLSN-TEST", "Description", "Message", DiagnosticSeverity.Warning, true, false, ""),
            TakeFormula = "builtincategory('OST_GenericModel')",
            GroupByFormula = "property('ViewSpecific')",
            IncludeLinks = includeLinks,
            IgnoreNestedFamilies = ignoreNestedFamilies,
        };
    }

    // A direct shape keeps exactly the solid it is given, unlike walls, which Revit joins and trims.
    private static DirectShape CreateBox(Document document, double minX, double minY, double sizeX, double sizeY)
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
            document, new ElementId(BuiltInCategory.OST_GenericModel));
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