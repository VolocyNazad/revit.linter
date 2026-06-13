using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.WallHeightIsTolerant;

internal sealed class WallHeightIsTolerantDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.WallHeightIsTolerant;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
