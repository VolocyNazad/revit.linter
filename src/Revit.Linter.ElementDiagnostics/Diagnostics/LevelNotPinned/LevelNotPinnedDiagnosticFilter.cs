namespace Revit.Linter.ElementDiagnostics.Diagnostics.LevelNotPinned;

internal sealed class LevelNotPinnedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LevelNotPinned;

    public bool IsRelevantFor(Document document, Element element) => element is Level;
}
