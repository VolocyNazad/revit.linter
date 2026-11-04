namespace Revit.Linter.ElementDiagnostics.Diagnostics.RevitLinkNotPinned;

internal sealed class RevitLinkNotPinnedDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RevitLinkNotPinned;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
