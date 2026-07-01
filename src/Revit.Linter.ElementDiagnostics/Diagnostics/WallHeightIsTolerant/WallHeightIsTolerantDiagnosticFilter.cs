namespace Revit.Linter.ElementDiagnostics.Diagnostics.WallHeightIsTolerant;

internal sealed class WallHeightIsTolerantDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.WallHeightIsTolerant;

    public bool IsRelevantFor(Document document, Element element) => element is Wall;
}