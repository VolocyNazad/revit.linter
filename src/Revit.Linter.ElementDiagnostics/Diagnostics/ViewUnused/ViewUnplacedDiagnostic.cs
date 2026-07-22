namespace Revit.Linter.ElementDiagnostics.Diagnostics.ViewUnused;

internal sealed class ViewUnplacedDiagnostic : IElementDiagnostic
{
    private readonly ElementFilter _viewportFilter = new ElementClassFilter(typeof(Viewport));

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ViewUnplaced;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        var targetView = (View)targetElement;
        var viewports = targetView.GetDependentElements(_viewportFilter);
        return viewports.Any() ? new(DiagnosticVerdict.Valid) : new(DiagnosticVerdict.NotValid);
    }
}
