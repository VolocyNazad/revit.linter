using Autodesk.Revit.DB.Architecture;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.RoomNotEnclosed;

internal sealed class RoomNotEnclosedDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RoomNotEnclosed;

    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
    {
        var room = (Room)targetElement;
        SpatialElementBoundaryOptions options = new()
        {
            SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish
        };
        var segments = room.GetBoundarySegments(options);
        return room.Area <= 0 && (segments == null || segments.Count == 0)
            ? new(DiagnosticVerdict.NotValid)
            : new(DiagnosticVerdict.Valid);
    }
}
