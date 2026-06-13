using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.WallHeightIsTolerant;

internal sealed class WallHeightIsTolerantDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.WallHeightIsTolerant;

    public bool IsRelevantFor(Document document, Element element) => element is Wall;
}