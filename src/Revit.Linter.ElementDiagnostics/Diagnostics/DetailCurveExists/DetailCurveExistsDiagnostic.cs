namespace Revit.Linter.ElementDiagnostics.Diagnostics.DetailCurveExists;

internal sealed class DetailCurveExistsDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DetailCurveExists;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {   
        return new(DiagnosticVerdict.NotValid);
    }
}
