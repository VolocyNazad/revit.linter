namespace Revit.Linter.DocumentDiagnostics.Diagnostics.PreviewNotSet;

internal sealed class PreviewNotSetDiagnostic : IDocumentDiagnostic
{
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.PreviewNotSet;

    public IEnumerable<DiagnosticFeedback> Execute(Document targetDocument)
    {
        yield return targetDocument.GetDocumentPreviewSettings().PreviewViewId == ElementId.InvalidElementId
            ? new(DiagnosticVerdict.NotValid)
            : DiagnosticFeedback.Valid;
    }
}
