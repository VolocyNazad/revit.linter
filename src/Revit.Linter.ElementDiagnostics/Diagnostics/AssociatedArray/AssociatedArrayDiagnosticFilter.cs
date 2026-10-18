namespace Revit.Linter.ElementDiagnostics.Diagnostics.AssociatedArray;

internal sealed class AssociatedArrayDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.AssociatedArray;

    public bool IsRelevantFor(Document document, Element element) => element is BaseArray;
}
