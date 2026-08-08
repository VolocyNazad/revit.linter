namespace Revit.Linter.DocumentDiagnostics.Diagnostics.RevitWarnings;

internal sealed class RevitWarningsDiagnosticFilter : IDocumentDiagnosticFilter
{
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.RevitWarnings;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
