namespace Revit.Linter.ElementDiagnostics.Diagnostics.ViewOnSheetWithoutTemplate;

internal sealed class ViewOnSheetWithoutTemplateDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ViewOnSheetWithoutTemplate;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
        => ((View)targetElement).ViewTemplateId == ElementId.InvalidElementId
            ? new(DiagnosticVerdict.NotValid)
            : new(DiagnosticVerdict.Valid);
}
