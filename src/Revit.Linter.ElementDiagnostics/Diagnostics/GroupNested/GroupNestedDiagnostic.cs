namespace Revit.Linter.ElementDiagnostics.Diagnostics.GroupNested;

internal sealed class GroupNestedDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.GroupNested;

    public IReadOnlyList<DiagnosticFeedback> Execute(Document document, View? view, Element targetElement)
        => DiagnosticFeedbacks.Of(targetElement.GroupId == ElementId.InvalidElementId
            ? new(DiagnosticVerdict.Valid)
            : new(DiagnosticVerdict.NotValid));
}
