using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.RoomNotEnclosed;

internal sealed class DeleteNotEnclosedRoom : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RoomNotEnclosed;

    public string Value => "Удалить неокруженное помещение";
    public bool Excecute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
