namespace Revit.Linter.DocumentDiagnostics.Diagnostics.MissingSizeTableColumn;

internal sealed class MissingSizeTableColumnDiagnosticFilter : IDocumentDiagnosticFilter
{
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.MissingSizeTableColumn;

    public bool IsRelevantFor(Document document) => document.IsFamilyDocument;
}
