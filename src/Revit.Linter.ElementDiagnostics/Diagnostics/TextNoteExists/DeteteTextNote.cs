namespace Revit.Linter.ElementDiagnostics.Diagnostics.TextNoteExists;

internal sealed class DeteteTextNote : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.TextNoteExists;

    public string Value => "Удалить текстовое примечание";
    public bool Execute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
