namespace Revit.Linter.ElementDiagnostics.Diagnostics.TextNoteExists;

internal sealed class DeleteTextNote : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.TextNoteExists;

    public string Value => ElementDiagnosticLocalizations.GetString("deleteTextNote_fix");
    public bool Execute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
