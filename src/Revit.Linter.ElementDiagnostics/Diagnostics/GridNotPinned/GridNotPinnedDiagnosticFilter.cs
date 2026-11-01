namespace Revit.Linter.ElementDiagnostics.Diagnostics.GridNotPinned;

internal sealed class GridNotPinnedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.GridNotPinned;

    public bool IsRelevantFor(Document document, Element element) => element is Grid or MultiSegmentGrid;
}
