namespace Revit.Linter.ElementDiagnostics.Diagnostics.TextNoteExists;

internal sealed class TextNoteExistsDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.TextNoteExists;

    public IReadOnlyList<DiagnosticFeedback> Execute(Document document, View? view, Element targetElement)
    {   
        return DiagnosticFeedbacks.Of(new(DiagnosticVerdict.NotValid));
    }
}
