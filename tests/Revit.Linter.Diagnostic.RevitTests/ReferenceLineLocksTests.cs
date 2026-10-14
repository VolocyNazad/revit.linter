using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.ReferenceLineUnused;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class ReferenceLineLocksTests : RevitApiTest
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
    public async Task Line_without_locks_is_reported()
    {
        Document document = _familyDocument!;
        ModelCurve line;
        using (Transaction transaction = new(document, "Create test reference line"))
        {
            transaction.Start();
            line = CreateReferenceLine(document, new XYZ(0, 0, 0), new XYZ(0, 10, 0));
            transaction.Commit();
        }
        var diagnostic = new ReferenceLineUnusedDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, line);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Line_with_locked_dimension_is_valid()
    {
        Document document = _familyDocument!;
        ViewPlan view = FindPlanView(document);
        ModelCurve first;
        ModelCurve second;
        using (Transaction transaction = new(document, "Create test locked dimension"))
        {
            transaction.Start();
            first = CreateReferenceLine(document, new XYZ(0, 0, 0), new XYZ(0, 10, 0));
            second = CreateReferenceLine(document, new XYZ(5, 0, 0), new XYZ(5, 10, 0));
            // The lines must be regenerated before their geometric references resolve.
            document.Regenerate();
            var references = new ReferenceArray();
            references.Append(first.GeometryCurve.GetEndPointReference(0));
            references.Append(second.GeometryCurve.GetEndPointReference(0));
            Dimension dimension = document.FamilyCreate.NewDimension(
                view, Line.CreateBound(new XYZ(0, -1, 0), new XYZ(5, -1, 0)), references);
            dimension.IsLocked = true;
            transaction.Commit();
        }
        var diagnostic = new ReferenceLineUnusedDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback firstFeedback = diagnostic.Execute(document, null, first);
        DiagnosticFeedback secondFeedback = diagnostic.Execute(document, null, second);

        await Assert.That(firstFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(secondFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Line_with_equalized_dimension_is_valid()
    {
        Document document = _familyDocument!;
        ViewPlan view = FindPlanView(document);
        ModelCurve first;
        ModelCurve middle;
        ModelCurve last;
        using (Transaction transaction = new(document, "Create test equalized dimension"))
        {
            transaction.Start();
            first = CreateReferenceLine(document, new XYZ(0, 0, 0), new XYZ(0, 10, 0));
            middle = CreateReferenceLine(document, new XYZ(5, 0, 0), new XYZ(5, 10, 0));
            last = CreateReferenceLine(document, new XYZ(10, 0, 0), new XYZ(10, 10, 0));
            // The lines must be regenerated before their geometric references resolve.
            document.Regenerate();
            var references = new ReferenceArray();
            references.Append(first.GeometryCurve.GetEndPointReference(0));
            references.Append(middle.GeometryCurve.GetEndPointReference(0));
            references.Append(last.GeometryCurve.GetEndPointReference(0));
            Dimension dimension = document.FamilyCreate.NewDimension(
                view, Line.CreateBound(new XYZ(0, -1, 0), new XYZ(10, -1, 0)), references);
            dimension.AreSegmentsEqual = true;
            transaction.Commit();
        }
        var diagnostic = new ReferenceLineUnusedDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback firstFeedback = diagnostic.Execute(document, null, first);
        DiagnosticFeedback middleFeedback = diagnostic.Execute(document, null, middle);
        DiagnosticFeedback lastFeedback = diagnostic.Execute(document, null, last);

        await Assert.That(firstFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(middleFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(lastFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Only_reference_lines_are_checked_by_filter()
    {
        Document document = _familyDocument!;
        ModelCurve reference;
        ModelCurve plain;
        using (Transaction transaction = new(document, "Create test lines"))
        {
            transaction.Start();
            reference = CreateReferenceLine(document, new XYZ(0, 0, 0), new XYZ(0, 10, 0));
            Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
            SketchPlane sketchPlane = SketchPlane.Create(document, plane);
            plain = document.FamilyCreate.NewModelCurve(
                Line.CreateBound(new XYZ(5, 0, 0), new XYZ(5, 10, 0)), sketchPlane);
            transaction.Commit();
        }
        var filter = new ReferenceLineUnusedDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, reference)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, plain)).IsFalse();
    }

    [Test]
    public async Task Check_runs_in_family_documents_only()
    {
        var filter = new ReferenceLineUnusedDiagnosticDocumentFilter();
        await Assert.That(filter.IsRelevantFor(_familyDocument!)).IsTrue();

        Document projectDocument = Application.NewProjectDocument(UnitSystem.Metric);
        try
        {
            await Assert.That(filter.IsRelevantFor(projectDocument)).IsFalse();
        }
        finally
        {
            projectDocument.Close(false);
        }
    }

    private static ModelCurve CreateReferenceLine(Document document, XYZ start, XYZ end)
    {
        Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
        SketchPlane sketchPlane = SketchPlane.Create(document, plane);
        ModelCurve line = document.FamilyCreate.NewModelCurve(Line.CreateBound(start, end), sketchPlane);
        line.ChangeToReferenceLine();
        return line;
    }

    private static ViewPlan FindPlanView(Document document) =>
        new FilteredElementCollector(document)
            .OfClass(typeof(ViewPlan))
            .Cast<ViewPlan>()
            .FirstOrDefault()
        ?? throw new InvalidOperationException("The test family has no plan view.");
}
