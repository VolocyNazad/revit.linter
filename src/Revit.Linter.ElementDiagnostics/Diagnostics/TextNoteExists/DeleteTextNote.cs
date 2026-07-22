namespace Revit.Linter.ElementDiagnostics.Diagnostics.TextNoteExists;

internal sealed class DeleteTextNote : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.TextNoteExists;

    public string Value => "Удалить текстовое примечание";
    public bool Execute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
