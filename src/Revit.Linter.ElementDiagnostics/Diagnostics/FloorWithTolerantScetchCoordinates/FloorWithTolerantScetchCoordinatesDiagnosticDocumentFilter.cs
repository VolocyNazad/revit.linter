#if AFTER2023
namespace Revit.Linter.ElementDiagnostics.Diagnostics.FloorWithTolerantScetchCoordinates;

internal sealed class FloorWithTolerantScetchCoordinatesDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FloorWithTolerantScetchCoordinates;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
#endif