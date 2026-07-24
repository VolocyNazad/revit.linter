namespace Revit.Linter.ElementDiagnostics.Diagnostics.GroupNested;

internal sealed class GroupNestedDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.GroupNested;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
        => targetElement.GroupId == ElementId.InvalidElementId
            ? new(DiagnosticVerdict.Valid)
            : new(DiagnosticVerdict.NotValid);
}
