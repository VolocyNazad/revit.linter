using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.ElementDiagnostics.Diagnostics.ReferenceLineUnused;
using Revit.Linter.ElementDiagnostics.Diagnostics.ReferencePlaneUnused;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class SharedDocumentQueriesTests : RevitApiTest
{
    private Document? _familyDocument;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() =>
        _familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _familyDocument?.Close(false);

    [Test]
    public async Task Reference_diagnostics_share_one_dimension_map()
    {
        // Both diagnostics read the dimensions-by-referenced-element entry built by whichever
        // runs first: one shared service proves the entry type stays compatible across the
        // two readers and that no cache key collides with another diagnostic's entry.
        Document document = _familyDocument!;
        ViewPlan view = new FilteredElementCollector(document)
            .OfClass(typeof(ViewPlan))
            .Cast<ViewPlan>()
            .FirstOrDefault()
            ?? throw new InvalidOperationException("The test family has no plan view.");
        ReferencePlane firstPlane;
        ReferencePlane secondPlane;
        ModelCurve firstLine;
        ModelCurve secondLine;
        using (Transaction transaction = new(document, "Create test constrained references"))
        {
            transaction.Start();
            firstPlane = document.FamilyCreate.NewReferencePlane(
                new XYZ(0, 0, 0), new XYZ(0, 10, 0), XYZ.BasisZ, view);
            secondPlane = document.FamilyCreate.NewReferencePlane(
                new XYZ(5, 0, 0), new XYZ(5, 10, 0), XYZ.BasisZ, view);
            firstLine = CreateReferenceLine(document, new XYZ(10, 0, 0), new XYZ(10, 10, 0));
            secondLine = CreateReferenceLine(document, new XYZ(15, 0, 0), new XYZ(15, 10, 0));
            // The references must be regenerated before they resolve in dimensions.
            document.Regenerate();
            var planeReferences = new ReferenceArray();
            planeReferences.Append(firstPlane.GetReference());
            planeReferences.Append(secondPlane.GetReference());
            Dimension planeDimension = document.FamilyCreate.NewDimension(
                view, Line.CreateBound(new XYZ(0, -1, 0), new XYZ(5, -1, 0)), planeReferences);
            planeDimension.IsLocked = true;
            var lineReferences = new ReferenceArray();
            lineReferences.Append(firstLine.GeometryCurve.GetEndPointReference(0));
            lineReferences.Append(secondLine.GeometryCurve.GetEndPointReference(0));
            Dimension lineDimension = document.FamilyCreate.NewDimension(
                view, Line.CreateBound(new XYZ(10, -1, 0), new XYZ(15, -1, 0)), lineReferences);
            lineDimension.IsLocked = true;
            transaction.Commit();
        }
        IDocumentQueryService queries = TestDocumentQueries.Create();
        var planeDiagnostic = new ReferencePlaneUnusedDiagnostic(queries);
        var lineDiagnostic = new ReferenceLineUnusedDiagnostic(queries);

        DiagnosticFeedback firstPlaneFeedback = planeDiagnostic.Execute(document, null, firstPlane);
        DiagnosticFeedback firstLineFeedback = lineDiagnostic.Execute(document, null, firstLine);
        DiagnosticFeedback secondPlaneFeedback = planeDiagnostic.Execute(document, null, secondPlane);
        DiagnosticFeedback secondLineFeedback = lineDiagnostic.Execute(document, null, secondLine);
        DocumentQueryStatistics mapStatistics = queries.TakeStatistics()
            .Single(item => item.Query == "element-diagnostics:dimensions-by-referenced-element");

        await Assert.That(firstPlaneFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(secondPlaneFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(firstLineFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(secondLineFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(mapStatistics.Misses).IsEqualTo(1);
        await Assert.That(mapStatistics.Hits).IsEqualTo(3);
    }

    private static ModelCurve CreateReferenceLine(Document document, XYZ start, XYZ end)
    {
        Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
        SketchPlane sketchPlane = SketchPlane.Create(document, plane);
        ModelCurve line = document.FamilyCreate.NewModelCurve(Line.CreateBound(start, end), sketchPlane);
        line.ChangeToReferenceLine();
        return line;
    }
}
