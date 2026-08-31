namespace Revit.Linter.ElementDiagnostics.Diagnostics.ImportInstanceExists;

internal sealed class ImportInstanceExistsDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ImportInstanceExists;

    public bool IsRelevantFor(Document document) => true;
}
