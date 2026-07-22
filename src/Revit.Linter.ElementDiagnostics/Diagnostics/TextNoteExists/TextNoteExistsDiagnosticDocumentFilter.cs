namespace Revit.Linter.ElementDiagnostics.Diagnostics.TextNoteExists;

internal sealed class TextNoteExistsDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.TextNoteExists;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
