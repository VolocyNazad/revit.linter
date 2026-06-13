using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.RoomIsRedundant;

internal sealed class RoomIsRedundantDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RoomIsRedundant;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
