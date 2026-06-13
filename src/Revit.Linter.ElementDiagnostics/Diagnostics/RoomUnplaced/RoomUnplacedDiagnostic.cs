using Autodesk.Revit.DB.Architecture;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.RoomUnplaced;

internal sealed class RoomUnpacedDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RoomUnplaced;

    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
    {
        var room = (Room)targetElement;
        return room.Location is null ? new(DiagnosticVerdict.NotValid) : new(DiagnosticVerdict.Valid);
    }
}
