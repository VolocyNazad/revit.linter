namespace Revit.Linter.ElementDiagnostics.Diagnostics.GroupExists;

internal sealed class GroupExistsDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.GroupExists;

    public bool IsRelevantFor(Document document) => true;
}
