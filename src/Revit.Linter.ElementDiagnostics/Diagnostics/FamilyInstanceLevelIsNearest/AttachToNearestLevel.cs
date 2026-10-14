using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyInstanceLevelIsNearest;

internal sealed class AttachToNearestLevel(IDocumentQueryService documentQueries) : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyInstanceLevelIsNearest;

    public string Value => ElementDiagnosticLocalizations.GetString("attachToNearestLevel_fix");
    public bool Execute(Element targetElement)
    {
        if (targetElement is not FamilyInstance familyInstance) return false;

        // Probes: an unreadable or read-only state means "cannot fix", not a run failure.
        Parameter? levelParameter = familyInstance.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
        if (levelParameter is null || levelParameter.IsReadOnly
            || levelParameter.StorageType != StorageType.ElementId) return false;
        ElementId currentLevelId = levelParameter.AsElementId();
        if (currentLevelId == ElementId.InvalidElementId) return false;
        if (targetElement.Document.GetElement(currentLevelId) is not Level currentLevel) return false;
        Parameter? elevationParameter = currentLevel.get_Parameter(BuiltInParameter.LEVEL_ELEV);
        if (elevationParameter is null || elevationParameter.StorageType != StorageType.Double) return false;
        Parameter? offsetParameter = familyInstance.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM);
        double offset = offsetParameter is not null && offsetParameter.StorageType == StorageType.Double
            ? offsetParameter.AsDouble()
            : 0;
        double targetElevation = elevationParameter.AsDouble() + offset;

        // Levels are shared through the transaction cache instead of a dedicated
        // collector per fix, and the minimum is a single linear pass: sorting is
        // O(L log L) with delegate and comparer allocations, while the minimum is O(L).
        // Manual loop rather than Enumerable.MinBy: MinBy is unavailable on net48 (Revit 2021-2023).
        ElementId? nearestLevelId = null;
        double nearestDistance = double.MaxValue;
        foreach (Level level in documentQueries.GetElementsOfClass<Level>(targetElement.Document))
        {
            Parameter? candidateElevation = level.get_Parameter(BuiltInParameter.LEVEL_ELEV);
            if (candidateElevation is null || candidateElevation.StorageType != StorageType.Double)
                continue;
            double distance = Math.Abs(candidateElevation.AsDouble() - targetElevation);
            if (distance >= nearestDistance)
                continue;
            nearestDistance = distance;
            nearestLevelId = level.Id;
        }
        if (nearestLevelId is null || nearestLevelId.Equals(ElementId.InvalidElementId)
            || nearestLevelId.Equals(currentLevelId)) return false;

        try
        {
            return levelParameter.Set(nearestLevelId);
        }
        catch (Exception)
        {
            // The level association can be rejected for instances whose level is driven
            // by their host or constraints: report as not applied, not as a failure.
            return false;
        }
    }
}
