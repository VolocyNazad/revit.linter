using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Sugar;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.ProfileFamilySymbolUnused;

internal sealed class ProfileFamilySymbolUnusedDiagnostic(
    IDocumentQueryService documentQueries) : IElementDiagnostic
{
    private const string UsedProfileSymbolIdsQuery = "element-diagnostics:used-profile-symbol-ids";

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ProfileFamilySymbolUnused;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        IReadOnlyCollection<ElementId> usedProfileSymbolIds = GetUsedProfileSymbolIds(document);
        return usedProfileSymbolIds.Contains(targetElement.Id)
            ? new(DiagnosticVerdict.Valid)
            : new(DiagnosticVerdict.NotValid);
    }

    private IReadOnlyCollection<ElementId> GetUsedProfileSymbolIds(Document document)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(document, UsedProfileSymbolIdsQuery),
            () => BuildUsedProfileSymbolIds(document));

    private HashSet<ElementId> BuildUsedProfileSymbolIds(Document document)
    {
        HashSet<ElementId> profileSymbolIds = documentQueries.GetElementsOfClass<FamilySymbol>(document)
            .Where(IsProfileSymbol)
            .Select(symbol => symbol.Id)
            .ToHashSet();
        var usedProfileSymbolIds = new HashSet<ElementId>();

        foreach (Element elementType in documentQueries.GetElementTypes(document))
        {
            foreach (Parameter parameter in elementType.Parameters)
            {
                if (parameter.StorageType != StorageType.ElementId) continue;

                ElementId referencedId = parameter.AsElementId();
                if (profileSymbolIds.Contains(referencedId))
                    usedProfileSymbolIds.Add(referencedId);
            }
        }

        return usedProfileSymbolIds;
    }

    private static bool IsProfileSymbol(FamilySymbol symbol)
        => symbol.Category?.Id.IsCategory(BuiltInCategory.OST_ProfileFamilies) == true;
}
