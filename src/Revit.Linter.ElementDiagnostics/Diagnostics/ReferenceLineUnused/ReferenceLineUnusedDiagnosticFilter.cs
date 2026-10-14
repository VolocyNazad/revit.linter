namespace Revit.Linter.ElementDiagnostics.Diagnostics.ReferenceLineUnused;

internal sealed class ReferenceLineUnusedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ReferenceLineUnused;

    public bool IsRelevantFor(Document document, Element element) =>
        element is ModelCurve modelCurve && modelCurve.IsReferenceLine;
}
