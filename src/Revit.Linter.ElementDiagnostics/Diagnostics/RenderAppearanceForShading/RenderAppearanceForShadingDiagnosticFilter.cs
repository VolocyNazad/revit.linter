namespace Revit.Linter.ElementDiagnostics.Diagnostics.RenderAppearanceForShading;

internal sealed class RenderAppearanceForShadingDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RenderAppearanceForShading;

    public bool IsRelevantFor(Document document, Element element) => element is Material;
}
