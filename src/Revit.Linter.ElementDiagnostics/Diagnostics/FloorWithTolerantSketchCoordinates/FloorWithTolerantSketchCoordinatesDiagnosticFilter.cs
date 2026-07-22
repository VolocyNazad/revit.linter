#if AFTER2023
namespace Revit.Linter.ElementDiagnostics.Diagnostics.FloorWithTolerantSketchCoordinates;

internal sealed class FloorWithTolerantSketchCoordinatesDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FloorWithTolerantSketchCoordinates;

    public bool IsRelevantFor(Document document, Element element) => element is Floor;
}

#endif