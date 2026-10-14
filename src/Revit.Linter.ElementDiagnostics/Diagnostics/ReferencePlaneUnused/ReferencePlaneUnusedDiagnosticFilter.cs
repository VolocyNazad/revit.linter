namespace Revit.Linter.ElementDiagnostics.Diagnostics.ReferencePlaneUnused;

internal sealed class ReferencePlaneUnusedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ReferencePlaneUnused;

    public bool IsRelevantFor(Document document, Element element) => element is ReferencePlane;
}
