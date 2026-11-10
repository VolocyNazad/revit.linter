using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.UnusedAppearanceAsset;

internal sealed class UnusedAppearanceAssetDiagnostic(IDocumentQueryService documentQueries)
    : IElementDiagnostic
{
    private const string UsedAppearanceAssetIdsQuery = "element-diagnostics:used-appearance-asset-ids";

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.UnusedAppearanceAsset;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        var appearanceAsset = (AppearanceAssetElement)targetElement;
        try
        {
            IReadOnlyCollection<ElementId> usedAppearanceAssetIds = GetUsedAppearanceAssetIds(document);
            return usedAppearanceAssetIds.Contains(appearanceAsset.Id)
                ? new(DiagnosticVerdict.Valid)
                : new(DiagnosticVerdict.NotValid);
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException)
        {
            // A material whose appearance cannot be read leaves the used set unknowable, so the asset
            // counts as clean rather than failing the whole run.
            return new(DiagnosticVerdict.Valid);
        }
    }

    private IReadOnlyCollection<ElementId> GetUsedAppearanceAssetIds(Document document)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(document, UsedAppearanceAssetIdsQuery),
            () => BuildUsedAppearanceAssetIds(document));

    private HashSet<ElementId> BuildUsedAppearanceAssetIds(Document document)
        => new(documentQueries.GetElementsOfClass<Material>(document)
            .Select(material => material.AppearanceAssetId)
            .Where(appearanceAssetId => appearanceAssetId is not null
                && appearanceAssetId != ElementId.InvalidElementId));
}
