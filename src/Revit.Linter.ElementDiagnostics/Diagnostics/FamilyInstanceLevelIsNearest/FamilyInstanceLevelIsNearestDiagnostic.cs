using Revit.TransactionMemoryCache.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyInstanceLevelIsNearest;

internal sealed class FamilyInstanceLevelIsNearestDiagnostic(
    IRevitTransactionMemoryCache revitTransactionMemoryCache) : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyInstanceLevelIsNearest;

    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
    {
        var familyInstance = (FamilyInstance)targetElement;
        ElementId levelId = familyInstance.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM).AsElementId();
        if (levelId == ElementId.InvalidElementId) return new(DiagnosticVerdict.Valid);
        var elevation = document.GetElement(levelId).get_Parameter(BuiltInParameter.LEVEL_ELEV).AsDouble();
        var elevationOffset = familyInstance.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).AsDouble();

        IList<Level> levels = revitTransactionMemoryCache
            .GetOrCreate(
                $"levels:document:{document.Title}",
                () => new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>().ToList())
            ?? throw new InvalidOperationException($"Failed to get object from cache.");

        ElementId nearedsLevelId = levels.OrderBy(level
            => Math.Abs(level.get_Parameter(BuiltInParameter.LEVEL_ELEV).AsDouble() - (elevation + elevationOffset))).First().Id;

        return nearedsLevelId != levelId
            ? new(DiagnosticVerdict.NotValid)
            : new(DiagnosticVerdict.Valid);
    }
}
