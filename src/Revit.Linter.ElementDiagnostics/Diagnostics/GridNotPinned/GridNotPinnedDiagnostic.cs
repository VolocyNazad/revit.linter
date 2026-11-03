namespace Revit.Linter.ElementDiagnostics.Diagnostics.GridNotPinned;

internal sealed class GridNotPinnedDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.GridNotPinned;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        return IsPinned(targetElement)
            ? new(DiagnosticVerdict.Valid) : new(DiagnosticVerdict.NotValid);
    }

    private static bool IsPinned(Element element)
    {
        if (element is MultiSegmentGrid multiSegmentGrid) return multiSegmentGrid.Pinned;
        if (element is not Grid grid) return true;

        // The grids of a multi-segment grid behave as one unit and share its pin state, so the
        // chain is checked once through the MultiSegmentGrid instead of once per segment.
        return MultiSegmentGrid.GetMultiSegementGridId(grid) != ElementId.InvalidElementId
            || grid.Pinned;
    }
}
