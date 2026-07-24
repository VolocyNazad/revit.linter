namespace Revit.Linter.ElementDiagnostics.Diagnostics.SheetWithMultipleTitleBlocks;

internal sealed class SheetWithMultipleTitleBlocksDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.SheetWithMultipleTitleBlocks;

    public bool IsRelevantFor(Document document, Element element) => element is ViewSheet;
}
