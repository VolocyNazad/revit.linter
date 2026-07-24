namespace Revit.Linter.ElementDiagnostics.Diagnostics.SheetEmpty;

internal sealed class SheetEmptyDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.SheetEmpty;

    public bool IsRelevantFor(Document document, Element element) => element is ViewSheet;
}
