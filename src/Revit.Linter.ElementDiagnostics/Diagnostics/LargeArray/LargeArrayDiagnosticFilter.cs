namespace Revit.Linter.ElementDiagnostics.Diagnostics.LargeArray;

internal sealed class LargeArrayDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LargeArray;

    public bool IsRelevantFor(Document document, Element element) => element is BaseArray;
}
