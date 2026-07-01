namespace Revit.Linter.ElementDiagnostics.Diagnostics.ParameterElementUnused;

internal sealed class ParameterElementUnusedDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ParameterElementUnused;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
