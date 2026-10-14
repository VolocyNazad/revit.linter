namespace Revit.Linter.DocumentDiagnostics.Diagnostics.PreviewNotSet;

internal sealed class PreviewNotSetDiagnosticFilter : IDocumentDiagnosticFilter
{
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.PreviewNotSet;

    public bool IsRelevantFor(Document document) => document.IsValidObject;
}
