using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.MaterialUnused;

/// <summary>
/// Builds the set of materials referenced by geometry, compound structures or painted faces.
/// </summary>
/// <remarks>
/// The set is cached under one document query key: the unused-material check (SHRD0022) and the
/// duplicate-material fix (SHRD0038) both ask for it in the same run, so the element scan happens once.
/// </remarks>
internal static class UsedMaterialIds
{
    private const string UsedMaterialIdsQuery = "element-diagnostics:used-material-ids";

    public static IReadOnlyCollection<ElementId> Get(IDocumentQueryService documentQueries, Document document)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(document, UsedMaterialIdsQuery),
            () => Build(documentQueries, document));

    private static HashSet<ElementId> Build(IDocumentQueryService documentQueries, Document document)
    {
        // GetMaterialIds(false) returns geometry/compound materials while GetMaterialIds(true)
        // returns painted face materials: the sets are disjoint, so both calls stay.
        IReadOnlyList<Element> elements = documentQueries.GetElements(document);
        var usedMaterialIds = new HashSet<ElementId>(elements.Count);
        foreach (Element element in elements)
        {
            foreach (ElementId materialId in element.GetMaterialIds(returnPaintMaterials: false))
                usedMaterialIds.Add(materialId);
            foreach (ElementId materialId in element.GetMaterialIds(returnPaintMaterials: true))
                usedMaterialIds.Add(materialId);
        }

        return usedMaterialIds;
    }
}
