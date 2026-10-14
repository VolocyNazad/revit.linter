namespace Revit.Linter.ElementDiagnostics.Diagnostics.GroupExists;

internal sealed class GroupExistsDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.GroupExists;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        return new(DiagnosticVerdict.NotValid);
    }
}
