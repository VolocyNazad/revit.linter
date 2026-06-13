using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.LevelHeightIsTolerant;

internal sealed class LevelHeightIsTolerantDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LevelHeightIsTolerant;

    public bool IsRelevantFor(Document document, Element element) => element is Level;
}