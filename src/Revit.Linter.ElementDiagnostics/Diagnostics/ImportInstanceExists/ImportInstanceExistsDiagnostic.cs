namespace Revit.Linter.ElementDiagnostics.Diagnostics.ImportInstanceExists;

internal sealed class ImportInstanceExistsDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ImportInstanceExists;

    public IReadOnlyList<DiagnosticFeedback> Execute(Document document, View? view, Element targetElement)
    {   
        return DiagnosticFeedbacks.Of(new(DiagnosticVerdict.NotValid));
    }
}
