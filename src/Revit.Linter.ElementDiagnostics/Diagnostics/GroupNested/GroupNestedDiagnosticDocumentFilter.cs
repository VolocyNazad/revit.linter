namespace Revit.Linter.ElementDiagnostics.Diagnostics.GroupNested;

internal sealed class GroupNestedDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.GroupNested;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
