namespace Revit.Linter.ElementDiagnostics.Diagnostics.RenderAppearanceForShading;

internal sealed class DisableRenderAppearanceForShading : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RenderAppearanceForShading;

    public string Value => ElementDiagnosticLocalizations.GetString("disableRenderAppearanceForShading_fix");

    public bool Execute(Element targetElement)
    {
        if (targetElement is not Material material) return false;
        if (!material.UseRenderAppearanceForShading) return false;
        material.UseRenderAppearanceForShading = false;
        return true;
    }
}
