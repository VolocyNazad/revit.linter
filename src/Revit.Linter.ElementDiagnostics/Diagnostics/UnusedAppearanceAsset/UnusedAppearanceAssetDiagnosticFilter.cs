namespace Revit.Linter.ElementDiagnostics.Diagnostics.UnusedAppearanceAsset;

internal sealed class UnusedAppearanceAssetDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.UnusedAppearanceAsset;

    public bool IsRelevantFor(Document document, Element element) => element is AppearanceAssetElement;
}
