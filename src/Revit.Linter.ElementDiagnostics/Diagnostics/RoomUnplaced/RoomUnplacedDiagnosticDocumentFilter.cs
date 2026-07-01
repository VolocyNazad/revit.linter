namespace Revit.Linter.ElementDiagnostics.Diagnostics.RoomUnplaced;

internal sealed class RoomUnplacedDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RoomUnplaced;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
