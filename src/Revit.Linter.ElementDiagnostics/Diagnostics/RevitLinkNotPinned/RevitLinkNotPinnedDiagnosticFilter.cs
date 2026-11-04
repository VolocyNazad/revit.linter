namespace Revit.Linter.ElementDiagnostics.Diagnostics.RevitLinkNotPinned;

internal sealed class RevitLinkNotPinnedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RevitLinkNotPinned;

    public bool IsRelevantFor(Document document, Element element) => element is RevitLinkInstance;
}
