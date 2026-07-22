namespace Revit.Linter.ElementDiagnostics.Diagnostics.ModelCurveExists;

internal sealed class ModelCurveExistsDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ModelCurveExists;

    public bool IsRelevantFor(Document document, Element element)
        => element is ModelCurve;
}
