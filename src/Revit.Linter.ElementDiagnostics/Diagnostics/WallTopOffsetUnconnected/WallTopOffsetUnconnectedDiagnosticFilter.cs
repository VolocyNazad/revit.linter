using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.WallTopOffsetUnconnected;

internal sealed class WallTopOffsetUnconnectedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.WallTopOffsetUnconnected;

    public bool IsRelevantFor(Document document, Element element) => element is Wall;
}
