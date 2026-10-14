using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.DocumentDiagnostics.Diagnostics.PreviewNotSet;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class PreviewViewTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Missing_preview_view_is_reported()
    {
        Document document = _document!;
        using (Transaction transaction = new(document, "Clear test preview view"))
        {
            transaction.Start();
            document.GetDocumentPreviewSettings().PreviewViewId = ElementId.InvalidElementId;
            transaction.Commit();
        }
        var diagnostic = new PreviewNotSetDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Assigned_preview_view_is_valid()
    {
        Document document = _document!;
        ElementId previewViewId;
        using (Transaction transaction = new(document, "Assign test preview view"))
        {
            transaction.Start();
            previewViewId = FindPreviewView(document).Id;
            document.GetDocumentPreviewSettings().PreviewViewId = previewViewId;
            transaction.Commit();
        }
        var diagnostic = new PreviewNotSetDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Deleted_preview_view_resets_to_missing()
    {
        Document document = _document!;
        ElementId previewViewId;
        using (Transaction transaction = new(document, "Assign test preview view"))
        {
            transaction.Start();
            previewViewId = FindPreviewView(document).Id;
            document.GetDocumentPreviewSettings().PreviewViewId = previewViewId;
            transaction.Commit();
        }
        using (Transaction transaction = new(document, "Delete test preview view"))
        {
            transaction.Start();
            document.Delete(previewViewId);
            transaction.Commit();
        }
        // Revit resets PreviewViewId to invalid when its view is deleted, so a dangling
        // non-invalid reference cannot be produced through the API. Deletion surfaces as DOC003.
        ElementId currentPreviewViewId = document.GetDocumentPreviewSettings().PreviewViewId;
        await Assert.That(currentPreviewViewId).IsEqualTo(ElementId.InvalidElementId);
        var diagnostic = new PreviewNotSetDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    [HookExecutor<RevitThreadExecutor>]
    public async Task Preview_checks_apply_to_projects_and_families()
    {
        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            await Assert.That(new PreviewNotSetDiagnosticFilter().IsRelevantFor(_document!)).IsTrue();
            await Assert.That(new PreviewNotSetDiagnosticFilter().IsRelevantFor(familyDocument)).IsTrue();
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    [Test]
    [HookExecutor<RevitThreadExecutor>]
    public async Task Fix_sets_the_active_view_as_preview()
    {
        Document document = _document!;
        bool applied;
        using (Transaction transaction = new(document, "Apply test preview fix"))
        {
            transaction.Start();
            applied = new SetActiveViewAsPreviewFix().Execute(document);
            transaction.Commit();
        }

        // Without a user interface the document may have no active view: then the fix refuses.
        // Otherwise it points the preview at the active view.
        await Assert.That(applied).IsEqualTo(document.ActiveView is not null);
        if (document.ActiveView is not null)
        {
            await Assert.That(document.GetDocumentPreviewSettings().PreviewViewId)
                .IsEqualTo(document.ActiveView.Id);
        }
    }

    private static View FindPreviewView(Document document)
    {
        DocumentPreviewSettings settings = document.GetDocumentPreviewSettings();
        View[] views = new FilteredElementCollector(document)
            .OfClass(typeof(View))
            .Cast<View>()
            .ToArray();
        return views.FirstOrDefault(view => settings.IsViewIdValidForPreview(view.Id))
            ?? throw new InvalidOperationException(
                "No preview-capable view was found. " +
                $"Available views: {string.Join(", ", views.Select(view => $"'{view.Name}'"))}.");
    }
}
