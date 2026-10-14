using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyInstanceLevelIsNearest;

internal sealed class FamilyInstanceLevelIsNearestDiagnostic(
    IDocumentQueryService documentQueries) : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyInstanceLevelIsNearest;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        var familyInstance = (FamilyInstance)targetElement;
        ElementId levelId = familyInstance.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM).AsElementId();
        if (levelId == ElementId.InvalidElementId) return new(DiagnosticVerdict.Valid);
        var elevation = document.GetElement(levelId).get_Parameter(BuiltInParameter.LEVEL_ELEV).AsDouble();
        var elevationOffset = familyInstance.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).AsDouble();

        IReadOnlyList<Level> levels = documentQueries.GetElementsOfClass<Level>(document);

        // Single linear pass instead of OrderBy(...).First(): sorting is O(L log L) with
        // delegate and comparer allocations per instance, while the minimum is O(L).
        // Manual loop rather than Enumerable.MinBy: MinBy is unavailable on net48 (Revit 2021-2023).
        double targetElevation = elevation + elevationOffset;
        ElementId nearestLevelId = levels[0].Id;
        double nearestDistance = double.MaxValue;
        foreach (Level level in levels)
        {
            double distance = Math.Abs(level.get_Parameter(BuiltInParameter.LEVEL_ELEV).AsDouble() - targetElevation);
            if (distance >= nearestDistance) continue;
            nearestDistance = distance;
            nearestLevelId = level.Id;
        }

        return nearestLevelId != levelId
            ? new(DiagnosticVerdict.NotValid)
            : new(DiagnosticVerdict.Valid);
    }
}