namespace Revit.Linter.ElementDiagnostics.Diagnostics.LargeRadialArray;

internal sealed class LargeRadialArrayDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LargeRadialArray;

    public bool IsRelevantFor(Document document) => true;
}
