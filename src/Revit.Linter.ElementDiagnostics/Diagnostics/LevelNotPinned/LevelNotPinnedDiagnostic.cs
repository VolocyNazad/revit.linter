namespace Revit.Linter.ElementDiagnostics.Diagnostics.LevelNotPinned;

internal sealed class LevelNotPinnedDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LevelNotPinned;

    public IReadOnlyList<DiagnosticFeedback> Execute(Document document, View? view, Element targetElement)
    {
        return DiagnosticFeedbacks.Of(targetElement is Level { Pinned: false }
            ? new(DiagnosticVerdict.NotValid) : new(DiagnosticVerdict.Valid));
    }
}
