namespace Revit.Linter.DocumentDiagnostics.Diagnostics.UnusedSizeTables;

internal sealed class UnusedSizeTablesDiagnosticFilter : IDocumentDiagnosticFilter
{
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.UnusedSizeTables;

    public bool IsRelevantFor(Document document) => document.IsFamilyDocument;
}
