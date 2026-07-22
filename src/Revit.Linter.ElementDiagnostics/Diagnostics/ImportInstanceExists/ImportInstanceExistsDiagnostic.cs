namespace Revit.Linter.ElementDiagnostics.Diagnostics.ImportInstanceExists;

internal sealed class ImportInstanceExistsDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ImportInstanceExists;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {   
        return new(DiagnosticVerdict.NotValid);
    }
}
