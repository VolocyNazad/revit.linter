using Revit.TransactionMemoryCache.Abstractions.Services;
using Toolkit.Revit.Extensions;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.ProfileFamilySymbolUnused;

internal sealed class ProfileFamilySymbolUnusedDiagnostic(
    IRevitTransactionMemoryCache revitTransactionMemoryCache) : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ProfileFamilySymbolUnused;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        IReadOnlyCollection<ElementId> usedProfileSymbolIds = GetUsedProfileSymbolIds(document);
        return usedProfileSymbolIds.Contains(targetElement.Id)
            ? new(DiagnosticVerdict.Valid)
            : new(DiagnosticVerdict.NotValid);
    }

    private IReadOnlyCollection<ElementId> GetUsedProfileSymbolIds(Document document)
        => revitTransactionMemoryCache.GetOrCreate(
               $"used-profile-symbols:document:{document.Title}",
               () => BuildUsedProfileSymbolIds(document))
           ?? throw new InvalidOperationException("Failed to get used profile symbols from cache.");

    private static HashSet<ElementId> BuildUsedProfileSymbolIds(Document document)
    {
        HashSet<ElementId> profileSymbolIds = new FilteredElementCollector(document)
            .OfClass(typeof(FamilySymbol))
            .OfType<FamilySymbol>()
            .Where(IsProfileSymbol)
            .Select(symbol => symbol.Id)
            .ToHashSet();
        var usedProfileSymbolIds = new HashSet<ElementId>();

        foreach (Element elementType in new FilteredElementCollector(document).WhereElementIsElementType())
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
