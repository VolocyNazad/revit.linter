namespace Revit.Linter.ElementDiagnostics.Diagnostics.GroupNested;

internal sealed class GroupNestedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.GroupNested;

    public bool IsRelevantFor(Document document, Element element) => element is Group;
}
