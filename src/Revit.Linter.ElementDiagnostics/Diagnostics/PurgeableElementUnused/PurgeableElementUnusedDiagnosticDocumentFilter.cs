namespace Revit.Linter.ElementDiagnostics.Diagnostics.PurgeableElementUnused;

internal sealed class PurgeableElementUnusedDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.PurgeableElementUnused;

    public bool IsRelevantFor(Document document) => true;
}
