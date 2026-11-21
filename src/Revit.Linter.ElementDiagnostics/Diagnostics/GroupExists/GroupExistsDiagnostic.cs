namespace Revit.Linter.ElementDiagnostics.Diagnostics.GroupExists;

internal sealed class GroupExistsDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.GroupExists;

    public IReadOnlyList<DiagnosticFeedback> Execute(Document document, View? view, Element targetElement)
    {
        return DiagnosticFeedbacks.Of(new(DiagnosticVerdict.NotValid));
    }
}
