using Revit.TransactionMemoryCache.Abstractions.Services;
using Toolkit.Revit.Extensions;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.MaterialUnused;

internal sealed class MaterialUnusedDiagnostic(IRevitTransactionMemoryCache revitTransactionMemoryCache)
    : IElementDiagnostic
{
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
        => revitTransactionMemoryCache
            .GetOrCreate($"usedMaterials:document:{document.Title}", () => BuildUsedMaterialIds(document))
            ?? throw new InvalidOperationException($"Failed to get object from cache.");

    private static HashSet<ElementId> BuildUsedMaterialIds(Document document)
    {
        var usedMaterialIds = new HashSet<ElementId>();
        foreach (Element element in new FilteredElementCollector(document)
            .WherePasses(ElementFilterUtils.AllFilter())
            .ToElements())
        {
            foreach (ElementId materialId in element.GetMaterialIds(returnPaintMaterials: false))
                usedMaterialIds.Add(materialId);
            foreach (ElementId materialId in element.GetMaterialIds(returnPaintMaterials: true))
                usedMaterialIds.Add(materialId);
        }

        return usedMaterialIds;
    }
}
