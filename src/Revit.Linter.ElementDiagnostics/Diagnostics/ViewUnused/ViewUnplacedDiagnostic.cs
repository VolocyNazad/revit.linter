namespace Revit.Linter.ElementDiagnostics.Diagnostics.ViewUnused;

internal sealed class ViewUnplacedDiagnostic : IElementDiagnostic
{
    private readonly ElementFilter viewportFilter = new ElementClassFilter(typeof(Viewport));

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ViewUnplaced;

    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
    {
        var targetView = (View)targetElement;
        var viewports = targetView.GetDependentElements(viewportFilter);
        return viewports.Any() ? new(DiagnosticVerdict.Valid) : new(DiagnosticVerdict.NotValid);
    }
}
