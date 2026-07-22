namespace Revit.Linter.ElementDiagnostics.Diagnostics.ModelCurveExists;

internal sealed class ModelCurveExistsDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ModelCurveExists;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {   
        return new(DiagnosticVerdict.NotValid);
    }
}
