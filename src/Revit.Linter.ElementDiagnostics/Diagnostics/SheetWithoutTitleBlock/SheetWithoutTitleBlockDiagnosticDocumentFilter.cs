namespace Revit.Linter.ElementDiagnostics.Diagnostics.SheetWithoutTitleBlock;

internal sealed class SheetWithoutTitleBlockDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.SheetWithoutTitleBlock;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
