namespace Revit.Linter.SizeTableDiagnostics.Diagnostics.UnusedSizeTables;

internal sealed class UnusedSizeTablesDiagnosticFilter : IDocumentDiagnosticFilter
{
    public DocumentDiagnosticId Identity => SizeTableDiagnosticIdCollector.UnusedSizeTables;

    public bool IsRelevantFor(Document document) => document.IsFamilyDocument;
}
