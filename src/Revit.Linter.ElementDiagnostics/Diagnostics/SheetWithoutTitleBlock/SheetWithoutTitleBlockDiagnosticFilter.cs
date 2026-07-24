namespace Revit.Linter.ElementDiagnostics.Diagnostics.SheetWithoutTitleBlock;

internal sealed class SheetWithoutTitleBlockDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.SheetWithoutTitleBlock;

    public bool IsRelevantFor(Document document, Element element) => element is ViewSheet;
}
