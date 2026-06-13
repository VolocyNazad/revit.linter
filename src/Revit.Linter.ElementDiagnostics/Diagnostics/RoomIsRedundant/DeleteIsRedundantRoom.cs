using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.RoomIsRedundant;

internal sealed class DeleteIsRedundantRoom : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RoomIsRedundant;

    public string Value => "Удалить избыточное помещение.";
    public bool Excecute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
