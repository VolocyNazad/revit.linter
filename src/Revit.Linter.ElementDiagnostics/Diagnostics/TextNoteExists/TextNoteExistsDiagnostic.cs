namespace Revit.Linter.ElementDiagnostics.Diagnostics.TextNoteExists;

internal sealed class TextNoteExistsDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.TextNoteExists;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {   
        return new(DiagnosticVerdict.NotValid);
    }
}
