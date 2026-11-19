namespace Revit.Linter.ElementDiagnostics.Diagnostics.LinkOnSharedWorkset;

internal sealed class LinkOnSharedWorksetDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LinkOnSharedWorkset;

    public bool IsRelevantFor(Document document, Element element) => LinkElement.IsLink(element);
}
