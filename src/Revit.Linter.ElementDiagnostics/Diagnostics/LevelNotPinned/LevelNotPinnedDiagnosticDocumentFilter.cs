namespace Revit.Linter.ElementDiagnostics.Diagnostics.LevelNotPinned;

internal sealed class LevelNotPinnedDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LevelNotPinned;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
