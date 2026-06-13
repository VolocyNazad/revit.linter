#if AFTER2023
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FloorWithTolerantScetchCoordinates;

internal sealed class FloorWithTolerantScetchCoordinatesDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FloorWithTolerantScetchCoordinates;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
#endif