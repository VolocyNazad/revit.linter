namespace Revit.Linter.ElementDiagnostics.Diagnostics.ViewOnSheetWithoutTemplate;

internal sealed class ViewOnSheetWithoutTemplateDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ViewOnSheetWithoutTemplate;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
