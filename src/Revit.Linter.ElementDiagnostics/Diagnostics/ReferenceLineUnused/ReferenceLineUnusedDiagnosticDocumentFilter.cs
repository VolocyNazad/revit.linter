namespace Revit.Linter.ElementDiagnostics.Diagnostics.ReferenceLineUnused;

internal sealed class ReferenceLineUnusedDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ReferenceLineUnused;

    public bool IsRelevantFor(Document document) => document.IsFamilyDocument;
}
