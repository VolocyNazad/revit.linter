namespace Revit.Linter.ElementDiagnostics.Diagnostics.ViewUnused;

internal sealed class ViewUnplacedDiagnostic : IElementDiagnostic
{
    private readonly ElementFilter _viewportFilter = new ElementClassFilter(typeof(Viewport));

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ViewUnplaced;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        var targetView = (View)targetElement;
        // Every view keeps a placeholder viewport that references the view itself
        // instead of a sheet: only a viewport on a real sheet means the view is placed.
        bool isPlaced = targetView.GetDependentElements(_viewportFilter)
            .Select(document.GetElement)
            .OfType<Viewport>()
            .Any(viewport => viewport.ViewId == targetView.Id
                && viewport.SheetId != ElementId.InvalidElementId);
        return isPlaced ? new(DiagnosticVerdict.Valid) : new(DiagnosticVerdict.NotValid);
    }
}
