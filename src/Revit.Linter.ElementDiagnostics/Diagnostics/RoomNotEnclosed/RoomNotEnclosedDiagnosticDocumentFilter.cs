using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.RoomNotEnclosed;

internal sealed class RoomNotEnclosedDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RoomNotEnclosed;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
