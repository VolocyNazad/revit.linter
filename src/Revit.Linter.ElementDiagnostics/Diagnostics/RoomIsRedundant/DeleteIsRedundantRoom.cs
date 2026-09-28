namespace Revit.Linter.ElementDiagnostics.Diagnostics.RoomIsRedundant;

internal sealed class DeleteIsRedundantRoom : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RoomIsRedundant;

    public string Value => ElementDiagnosticLocalizations.GetString("deleteRedundantRoom_fix");
    public bool Execute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
