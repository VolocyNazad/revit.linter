namespace Revit.Linter.ElementDiagnostics.Diagnostics.RenderAppearanceForShading;

internal sealed class RenderAppearanceForShadingDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RenderAppearanceForShading;

    public bool IsRelevantFor(Document document) => true;
}
