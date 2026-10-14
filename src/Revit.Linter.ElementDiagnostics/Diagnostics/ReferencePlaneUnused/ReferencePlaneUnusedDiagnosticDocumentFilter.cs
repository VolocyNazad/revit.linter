namespace Revit.Linter.ElementDiagnostics.Diagnostics.ReferencePlaneUnused;

internal sealed class ReferencePlaneUnusedDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ReferencePlaneUnused;

    public bool IsRelevantFor(Document document) => document.IsFamilyDocument;
}
