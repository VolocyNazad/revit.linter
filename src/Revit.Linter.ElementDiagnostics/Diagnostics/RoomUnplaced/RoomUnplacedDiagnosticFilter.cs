using Autodesk.Revit.DB.Architecture;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.RoomUnplaced;

internal sealed class RoomUnplacedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RoomUnplaced;

    public bool IsRelevantFor(Document document, Element element) => element is Room;
}