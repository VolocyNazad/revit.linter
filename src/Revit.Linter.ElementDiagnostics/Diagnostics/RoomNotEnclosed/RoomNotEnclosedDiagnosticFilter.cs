using Autodesk.Revit.DB.Architecture;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.RoomNotEnclosed;

internal sealed class RoomNotEnclosedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RoomNotEnclosed;

    public bool IsRelevantFor(Document document, Element element) => element is Room;
}