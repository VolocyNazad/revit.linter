namespace Revit.Linter.ElementDiagnostics.Diagnostics.TextNoteExists;

internal sealed class TextNoteExistsDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.TextNoteExists;

    public bool IsRelevantFor(Document document, Element element)
        => element is TextNote;
}
