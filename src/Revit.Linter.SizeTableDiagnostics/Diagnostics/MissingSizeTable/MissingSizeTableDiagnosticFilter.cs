namespace Revit.Linter.SizeTableDiagnostics.Diagnostics.MissingSizeTable;

internal sealed class MissingSizeTableDiagnosticFilter : IDocumentDiagnosticFilter
{
    public DocumentDiagnosticId Identity => SizeTableDiagnosticIdCollector.MissingSizeTable;

    public bool IsRelevantFor(Document document) => document.IsFamilyDocument;
}
