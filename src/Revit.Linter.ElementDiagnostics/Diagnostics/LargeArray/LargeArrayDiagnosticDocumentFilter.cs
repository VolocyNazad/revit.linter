namespace Revit.Linter.ElementDiagnostics.Diagnostics.LargeArray;

internal sealed class LargeArrayDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LargeArray;

    public bool IsRelevantFor(Document document) => true;
}
