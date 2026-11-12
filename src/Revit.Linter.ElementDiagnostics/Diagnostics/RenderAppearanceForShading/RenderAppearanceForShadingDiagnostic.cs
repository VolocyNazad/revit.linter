using Autodesk.Revit.DB.Visual;
using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.RenderAppearanceForShading;

internal sealed class RenderAppearanceForShadingDiagnostic(IDocumentQueryService documentQueries)
    : IElementDiagnostic
{
    private const string AppearanceAssetTextureQuery = "element-diagnostics:appearance-asset-has-texture";

    private static readonly string[] TextureChannels =
    [
        Generic.GenericDiffuse,
        Generic.GenericBumpMap,
        Generic.GenericTransparency,
        Generic.GenericGlossiness,
    ];

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RenderAppearanceForShading;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        var material = (Material)targetElement;
        if (!material.UseRenderAppearanceForShading) return new(DiagnosticVerdict.Valid);
        ElementId appearanceAssetId = material.AppearanceAssetId;
        if (appearanceAssetId == ElementId.InvalidElementId) return new(DiagnosticVerdict.Valid);
        return GetTextureClassification(document).TryGetValue(appearanceAssetId, out bool hasTexture) && hasTexture
            ? new(DiagnosticVerdict.NotValid)
            : new(DiagnosticVerdict.Valid);
    }

    private Dictionary<ElementId, bool> GetTextureClassification(Document document)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(document, AppearanceAssetTextureQuery),
            () => BuildTextureClassification(document));

    private Dictionary<ElementId, bool> BuildTextureClassification(Document document)
    {
        // Asset inspection is the expensive part of the diagnostic, so every appearance asset of
        // the document is classified once and the per-material pass only looks the id up.
        IReadOnlyList<AppearanceAssetElement> appearanceAssets =
            documentQueries.GetElementsOfClass<AppearanceAssetElement>(document);
        var hasTextureById = new Dictionary<ElementId, bool>(appearanceAssets.Count);
        foreach (AppearanceAssetElement appearanceAsset in appearanceAssets)
            hasTextureById[appearanceAsset.Id] = HasTexture(appearanceAsset);
        return hasTextureById;
    }

    private static bool HasTexture(AppearanceAssetElement appearanceAsset)
    {
        try
        {
            Asset asset = appearanceAsset.GetRenderingAsset();
            if (asset.Size == 0) return false;
            return TextureChannels.Any(channel => HasConnectedProperties(asset, channel));
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException)
        {
            // An appearance asset Revit cannot read is clean, not a run failure.
            return false;
        }
    }

    private static bool HasConnectedProperties(AssetProperties asset, string channel)
        => asset.FindByName(channel) is { NumberOfConnectedProperties: > 0 };
}
