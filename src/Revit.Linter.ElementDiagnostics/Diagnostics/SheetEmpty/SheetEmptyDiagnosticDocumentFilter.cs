namespace Revit.Linter.ElementDiagnostics.Diagnostics.SheetEmpty;

internal sealed class SheetEmptyDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.SheetEmpty;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
