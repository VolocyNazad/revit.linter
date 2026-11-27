namespace Revit.Linter.DocumentDiagnostics.Diagnostics.MissingSizeTable;

internal sealed class MissingSizeTableDiagnosticFilter : IDocumentDiagnosticFilter
{
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.MissingSizeTable;

    public bool IsRelevantFor(Document document) => document.IsFamilyDocument;
}
