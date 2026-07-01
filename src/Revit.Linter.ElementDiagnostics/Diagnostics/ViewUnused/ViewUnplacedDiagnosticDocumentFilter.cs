namespace Revit.Linter.ElementDiagnostics.Diagnostics.ViewUnused;

internal sealed class ViewUnplacedDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ViewUnplaced;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
