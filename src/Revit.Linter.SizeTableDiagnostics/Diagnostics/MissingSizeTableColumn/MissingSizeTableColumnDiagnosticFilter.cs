namespace Revit.Linter.SizeTableDiagnostics.Diagnostics.MissingSizeTableColumn;

internal sealed class MissingSizeTableColumnDiagnosticFilter : IDocumentDiagnosticFilter
{
    public DocumentDiagnosticId Identity => SizeTableDiagnosticIdCollector.MissingSizeTableColumn;

    public bool IsRelevantFor(Document document) => document.IsFamilyDocument;
}
