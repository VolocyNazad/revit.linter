namespace Revit.Linter.ElementDiagnostics.Diagnostics.LargeRadialArray;

internal sealed class LargeRadialArrayDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LargeRadialArray;

    public bool IsRelevantFor(Document document, Element element) => element is RadialArray;
}
