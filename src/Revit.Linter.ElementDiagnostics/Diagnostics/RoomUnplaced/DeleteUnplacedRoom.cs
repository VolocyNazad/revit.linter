namespace Revit.Linter.ElementDiagnostics.Diagnostics.RoomUnplaced;

internal sealed class DeleteUnplacedRoom : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RoomUnplaced;

    public string Value => ElementDiagnosticLocalizations.GetString("deleteUnplacedRoom_fix");
    public bool Execute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
