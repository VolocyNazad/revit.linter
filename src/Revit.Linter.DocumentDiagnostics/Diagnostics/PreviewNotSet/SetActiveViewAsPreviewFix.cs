namespace Revit.Linter.DocumentDiagnostics.Diagnostics.PreviewNotSet;

internal sealed class SetActiveViewAsPreviewFix : IDocumentFix
{
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.PreviewNotSet;

    public string Value => DocumentDiagnosticLocalizations.GetString("setActiveViewAsPreview_fix");

    public bool Execute(Document targetDocument)
    {
        ElementId? activeViewId = targetDocument.ActiveView?.Id;
        if (activeViewId is null) return false;
        DocumentPreviewSettings settings = targetDocument.GetDocumentPreviewSettings();
        if (!settings.IsViewIdValidForPreview(activeViewId)) return false;
        settings.PreviewViewId = activeViewId;
        return true;
    }
}
