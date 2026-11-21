namespace Revit.Linter.ElementDiagnostics.Diagnostics.DetailCurveExists;

internal sealed class DetailCurveExistsDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DetailCurveExists;

    public IReadOnlyList<DiagnosticFeedback> Execute(Document document, View? view, Element targetElement)
    {   
        return DiagnosticFeedbacks.Of(new(DiagnosticVerdict.NotValid));
    }
}
