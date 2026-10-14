using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.MaterialUnused;

internal sealed class MaterialUnusedDiagnostic(IDocumentQueryService documentQueries)
    : IElementDiagnostic
{
    private const string UsedMaterialIdsQuery = "element-diagnostics:used-material-ids";

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.MaterialUnused;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        var material = (Material)targetElement;
        IReadOnlyCollection<ElementId> usedMaterialIds = GetUsedMaterialIds(document);
        return usedMaterialIds.Contains(material.Id)
            ? new(DiagnosticVerdict.Valid)
            : new(DiagnosticVerdict.NotValid);
    }

    private IReadOnlyCollection<ElementId> GetUsedMaterialIds(Document document)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(document, UsedMaterialIdsQuery),
            () => BuildUsedMaterialIds(document));

    private HashSet<ElementId> BuildUsedMaterialIds(Document document)
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