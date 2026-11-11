namespace Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateMaterial;

internal sealed class DuplicateMaterialDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DuplicateMaterial;

    public bool IsRelevantFor(Document document, Element element) => element is Material;
}
