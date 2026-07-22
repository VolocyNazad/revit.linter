namespace Revit.Linter.ElementDiagnostics.Diagnostics.ImportInstanceExists;

internal sealed class ImportInstanceExistsDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ImportInstanceExists;

    public bool IsRelevantFor(Document document, Element element)
        => element is ImportInstance;
}
