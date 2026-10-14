namespace Revit.Linter.ElementDiagnostics.Diagnostics.GroupExists;

internal sealed class GroupExistsDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.GroupExists;

    public bool IsRelevantFor(Document document, Element element) => element is Group;
}
