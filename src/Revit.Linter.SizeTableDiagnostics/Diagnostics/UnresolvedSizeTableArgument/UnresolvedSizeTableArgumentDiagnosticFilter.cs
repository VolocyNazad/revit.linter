namespace Revit.Linter.SizeTableDiagnostics.Diagnostics.UnresolvedSizeTableArgument;

internal sealed class UnresolvedSizeTableArgumentDiagnosticFilter : IDocumentDiagnosticFilter
{
    public DocumentDiagnosticId Identity => SizeTableDiagnosticIdCollector.UnresolvedSizeTableArgument;

    public bool IsRelevantFor(Document document) => document.IsFamilyDocument;
}
