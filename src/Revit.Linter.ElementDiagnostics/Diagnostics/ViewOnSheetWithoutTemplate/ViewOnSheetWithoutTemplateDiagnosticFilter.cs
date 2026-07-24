namespace Revit.Linter.ElementDiagnostics.Diagnostics.ViewOnSheetWithoutTemplate;

internal sealed class ViewOnSheetWithoutTemplateDiagnosticFilter : IElementDiagnosticFilter
{
    private readonly ElementFilter _viewportFilter = new ElementClassFilter(typeof(Viewport));

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ViewOnSheetWithoutTemplate;

    public bool IsRelevantFor(Document document, Element element)
        => element is View { IsTemplate: false, ViewType: not ViewType.DrawingSheet } targetView
           && targetView.GetDependentElements(_viewportFilter).Count > 0;
}
