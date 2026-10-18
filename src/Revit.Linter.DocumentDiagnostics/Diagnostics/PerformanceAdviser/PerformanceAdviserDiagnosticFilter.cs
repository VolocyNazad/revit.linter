namespace Revit.Linter.DocumentDiagnostics.Diagnostics.PerformanceAdviser;

internal sealed class PerformanceAdviserDiagnosticFilter : IDocumentDiagnosticFilter
{
    // Performance Adviser rules cover both project and family documents.
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.PerformanceAdviser;

    public bool IsRelevantFor(Document document) => true;
}
