namespace Revit.Linter.ElementDiagnostics.Diagnostics.LinkOnSharedWorkset;

internal sealed class LinkOnSharedWorksetDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LinkOnSharedWorkset;

    // Worksets exist only in workshared projects: a family document has none, and a project
    // without worksharing keeps every element in a single implicit workset that the rule cannot
    // separate, so there is nothing to check in either.
    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument && document.IsWorkshared;
}
