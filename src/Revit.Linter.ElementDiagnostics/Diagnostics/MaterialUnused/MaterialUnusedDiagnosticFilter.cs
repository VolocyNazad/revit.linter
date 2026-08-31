namespace Revit.Linter.ElementDiagnostics.Diagnostics.MaterialUnused;

internal sealed class MaterialUnusedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.MaterialUnused;

    public bool IsRelevantFor(Document document, Element element) => element is Material;
}