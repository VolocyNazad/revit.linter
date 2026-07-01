using Autodesk.Revit.DB.Architecture;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.RoomIsRedundant;

internal sealed class RoomIsRedundantDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RoomIsRedundant;

    public bool IsRelevantFor(Document document, Element element) => element is Room;
}