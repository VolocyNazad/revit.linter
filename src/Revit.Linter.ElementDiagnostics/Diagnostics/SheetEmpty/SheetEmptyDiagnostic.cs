namespace Revit.Linter.ElementDiagnostics.Diagnostics.SheetEmpty;

internal sealed class SheetEmptyDiagnostic : IElementDiagnostic
{
    private readonly ElementFilter _sheetContentFilter = new LogicalOrFilter([
        new ElementClassFilter(typeof(Viewport)),
        new ElementClassFilter(typeof(ScheduleSheetInstance))]);

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.SheetEmpty;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
        => targetElement.GetDependentElements(_sheetContentFilter).Count > 0
            ? new(DiagnosticVerdict.Valid)
            : new(DiagnosticVerdict.NotValid);
}
