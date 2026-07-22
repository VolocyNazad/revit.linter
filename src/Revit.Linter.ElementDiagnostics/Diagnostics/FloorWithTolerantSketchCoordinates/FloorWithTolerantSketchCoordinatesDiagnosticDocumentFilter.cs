#if AFTER2023
namespace Revit.Linter.ElementDiagnostics.Diagnostics.FloorWithTolerantSketchCoordinates;

internal sealed class FloorWithTolerantSketchCoordinatesDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FloorWithTolerantSketchCoordinates;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
#endif