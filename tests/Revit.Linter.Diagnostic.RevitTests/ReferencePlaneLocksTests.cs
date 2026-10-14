using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.ReferencePlaneUnused;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class ReferencePlaneLocksTests : RevitApiTest
{
    private Document? _familyDocument;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() =>
        _familyDocument = FamilyTestDocuments.OpenTemplateFamily(Application);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _familyDocument?.Close(false);

    [Test]
    public async Task Plane_without_locks_is_reported()
    {
        ReferencePlane plane = new FilteredElementCollector(_familyDocument!)
            .OfClass(typeof(ReferencePlane))
            .Cast<ReferencePlane>()
            .First();
        var diagnostic = new ReferencePlaneUnusedDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(_familyDocument!, null, plane);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Plane_with_locked_dimension_is_valid()
    {
        Document document = _familyDocument!;
        ViewPlan view = new FilteredElementCollector(document)
            .OfClass(typeof(ViewPlan))
            .Cast<ViewPlan>()
            .FirstOrDefault()
            ?? throw new InvalidOperationException("The test family has no plan view.");
        ReferencePlane first;
        ReferencePlane second;
        using (Transaction transaction = new(document, "Create test reference planes"))
        {
            transaction.Start();
            first = document.FamilyCreate.NewReferencePlane(
                new XYZ(0, 0, 0), new XYZ(0, 10, 0), XYZ.BasisZ, view);
            second = document.FamilyCreate.NewReferencePlane(
                new XYZ(5, 0, 0), new XYZ(5, 10, 0), XYZ.BasisZ, view);
            // The planes must be regenerated before their geometric references resolve.
            document.Regenerate();
            var references = new ReferenceArray();
            references.Append(first.GetReference());
            references.Append(second.GetReference());
            Dimension dimension = document.FamilyCreate.NewDimension(
                view, Line.CreateBound(new XYZ(0, -1, 0), new XYZ(5, -1, 0)), references);
            dimension.IsLocked = true;
            transaction.Commit();
        }
        var diagnostic = new ReferencePlaneUnusedDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback firstFeedback = diagnostic.Execute(document, null, first);
        DiagnosticFeedback secondFeedback = diagnostic.Execute(document, null, second);

        await Assert.That(firstFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(secondFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Plane_with_equalized_dimension_is_valid()
    {
        Document document = _familyDocument!;
        ViewPlan view = new FilteredElementCollector(document)
            .OfClass(typeof(ViewPlan))
            .Cast<ViewPlan>()
            .FirstOrDefault()
            ?? throw new InvalidOperationException("The test family has no plan view.");
        ReferencePlane first;
        ReferencePlane middle;
        ReferencePlane last;
        using (Transaction transaction = new(document, "Create test equalized dimension"))
        {
            transaction.Start();
            first = document.FamilyCreate.NewReferencePlane(
                new XYZ(0, 0, 0), new XYZ(0, 10, 0), XYZ.BasisZ, view);
            middle = document.FamilyCreate.NewReferencePlane(
                new XYZ(5, 0, 0), new XYZ(5, 10, 0), XYZ.BasisZ, view);
            last = document.FamilyCreate.NewReferencePlane(
                new XYZ(10, 0, 0), new XYZ(10, 10, 0), XYZ.BasisZ, view);
            // The planes must be regenerated before their geometric references resolve.
            document.Regenerate();
            var references = new ReferenceArray();
            references.Append(first.GetReference());
            references.Append(middle.GetReference());
            references.Append(last.GetReference());
            Dimension dimension = document.FamilyCreate.NewDimension(
                view, Line.CreateBound(new XYZ(0, -1, 0), new XYZ(10, -1, 0)), references);
            dimension.AreSegmentsEqual = true;
            transaction.Commit();
        }
        var diagnostic = new ReferencePlaneUnusedDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback firstFeedback = diagnostic.Execute(document, null, first);
        DiagnosticFeedback middleFeedback = diagnostic.Execute(document, null, middle);
        DiagnosticFeedback lastFeedback = diagnostic.Execute(document, null, last);

        await Assert.That(firstFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(middleFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(lastFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Only_reference_planes_are_checked_by_filter()
    {
        Document document = _familyDocument!;
        ReferencePlane plane = new FilteredElementCollector(document)
            .OfClass(typeof(ReferencePlane))
            .Cast<ReferencePlane>()
            .First();
        var filter = new ReferencePlaneUnusedDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, plane)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, document.OwnerFamily)).IsFalse();
    }

    [Test]
    public async Task Check_runs_in_family_documents_only()
    {
        var filter = new ReferencePlaneUnusedDiagnosticDocumentFilter();
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
}
