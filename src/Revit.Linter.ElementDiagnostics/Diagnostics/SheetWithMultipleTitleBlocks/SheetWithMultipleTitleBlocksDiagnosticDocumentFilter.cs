namespace Revit.Linter.ElementDiagnostics.Diagnostics.SheetWithMultipleTitleBlocks;

internal sealed class SheetWithMultipleTitleBlocksDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.SheetWithMultipleTitleBlocks;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
