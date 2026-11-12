namespace Revit.Linter.ElementDiagnostics.Diagnostics.ViewHasHiddenElements;

internal sealed class ViewHasHiddenElementsDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ViewHasHiddenElements;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
