namespace Revit.Linter.ElementDiagnostics.Diagnostics.RoomUnplaced;

internal sealed class DeleteUnplacedRoom : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RoomUnplaced;

    public string Value => "Удалить неразмещенное помещение";
    public bool Excecute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
