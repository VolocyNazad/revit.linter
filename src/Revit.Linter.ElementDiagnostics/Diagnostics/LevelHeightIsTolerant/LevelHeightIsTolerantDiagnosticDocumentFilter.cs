using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.LevelHeightIsTolerant;

internal sealed class LevelHeightIsTolerantDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LevelHeightIsTolerant;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
