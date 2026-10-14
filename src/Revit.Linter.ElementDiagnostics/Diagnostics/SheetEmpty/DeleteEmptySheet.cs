namespace Revit.Linter.ElementDiagnostics.Diagnostics.SheetEmpty;

internal sealed class DeleteEmptySheet : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.SheetEmpty;

    public string Value => ElementDiagnosticLocalizations.GetString("deleteEmptySheet_fix");
    public bool Execute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
