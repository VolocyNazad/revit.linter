namespace Revit.Linter.ElementDiagnostics.Diagnostics.GridNotPinned;

internal sealed class GridNotPinnedDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.GridNotPinned;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
