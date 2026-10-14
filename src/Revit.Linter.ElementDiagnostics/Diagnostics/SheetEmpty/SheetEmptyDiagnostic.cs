namespace Revit.Linter.ElementDiagnostics.Diagnostics.SheetEmpty;

internal sealed class SheetEmptyDiagnostic : IElementDiagnostic
{
    private readonly ElementFilter _sheetContentFilter = new LogicalOrFilter([
        new ElementClassFilter(typeof(Viewport)),
        new ElementClassFilter(typeof(ScheduleSheetInstance))]);

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.SheetEmpty;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        // Every sheet keeps a placeholder viewport that references the sheet itself
        // instead of a placed view: only viewports on this sheet showing another view,
        // and any schedule instances, mean the sheet is not empty.
        bool hasContent = targetElement.GetDependentElements(_sheetContentFilter)
            .Select(document.GetElement)
            .Any(element => element is ScheduleSheetInstance
                || (element is Viewport viewport
                    && viewport.SheetId == targetElement.Id
                    && viewport.ViewId != targetElement.Id));
        return hasContent
            ? new(DiagnosticVerdict.Valid)
            : new(DiagnosticVerdict.NotValid);
    }
}
