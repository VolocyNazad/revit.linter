namespace Revit.Linter.ElementDiagnostics.Diagnostics.RoomNotEnclosed;

internal sealed class DeleteNotEnclosedRoom : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RoomNotEnclosed;

    public string Value => ElementDiagnosticLocalizations.GetString("deleteNotEnclosedRoom_fix");
    public bool Execute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
