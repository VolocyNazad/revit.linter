#if AFTER2023
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FloorWithTolerantScetchCoordinates;

internal sealed class FloorWithTolerantScetchCoordinatesDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FloorWithTolerantScetchCoordinates;

    public bool IsRelevantFor(Document document, Element element) => element is Floor;
}

#endif