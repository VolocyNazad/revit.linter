using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyInstanceLevelIsNearest;

internal sealed class FamilyInstanceLevelIsNearestDiagnostic(
    IDocumentQueryService documentQueries) : IElementDiagnostic
{
    private const string LevelElevationsQuery = "element-diagnostics:level-elevations";

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyInstanceLevelIsNearest;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        var familyInstance = (FamilyInstance)targetElement;
        ElementId levelId = familyInstance.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM).AsElementId();
        if (levelId == ElementId.InvalidElementId) return new(DiagnosticVerdict.Valid);
        var elevation = document.GetElement(levelId).get_Parameter(BuiltInParameter.LEVEL_ELEV).AsDouble();
        var elevationOffset = familyInstance.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).AsDouble();

        (ElementId id, double elevation)[] levels = GetLevelElevations(document);
        if (levels.Length == 0) return new(DiagnosticVerdict.Valid);

        // Single linear pass instead of OrderBy(...).First(): sorting is O(L log L) with
        // delegate and comparer allocations per instance, while the minimum is O(L).
        // Manual loop rather than Enumerable.MinBy: MinBy is unavailable on net48 (Revit 2021-2023).
        double targetElevation = elevation + elevationOffset;
        ElementId nearestLevelId = levels[0].id;
        double nearestDistance = double.MaxValue;
        foreach ((ElementId id, double elevation) level in levels)
        {
            double distance = Math.Abs(level.elevation - targetElevation);
            if (distance >= nearestDistance) continue;
            nearestDistance = distance;
            nearestLevelId = level.id;
        }

        return nearestLevelId != levelId
            ? new(DiagnosticVerdict.NotValid)
            : new(DiagnosticVerdict.Valid);
    }

    private (ElementId id, double elevation)[] GetLevelElevations(Document document)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(document, LevelElevationsQuery),
            () => BuildLevelElevations(document));

    private (ElementId id, double elevation)[] BuildLevelElevations(Document document)
    {
        // Levels do not change during a run, so elevations are read once: previously every
        // instance reread the elevation of every level (I x L native calls).
        IReadOnlyList<Level> levels = documentQueries.GetElementsOfClass<Level>(document);
        var elevations = new (ElementId id, double elevation)[levels.Count];
        for (int i = 0; i < levels.Count; i++)
            elevations[i] = (levels[i].Id, levels[i].get_Parameter(BuiltInParameter.LEVEL_ELEV).AsDouble());
        return elevations;
    }
}