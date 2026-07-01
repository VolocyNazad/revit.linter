namespace Revit.Linter.DocumentDiagnostics.Diagnostics.StartingViewNotSet;

internal sealed class StartingViewNotSetDiagnosticFilter : IDocumentDiagnosticFilter
{
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.StartingViewNotSet;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
