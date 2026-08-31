namespace Revit.Linter.ElementDiagnostics.Diagnostics.MaterialUnused;

internal sealed class MaterialUnusedDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.MaterialUnused;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}