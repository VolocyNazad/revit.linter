namespace Revit.Linter.CollisionDiagnostics.Infrastructure;

/// <summary>Names the document queries the collision module caches.</summary>
/// <remarks>
/// The cache key carries the name as a string, while the closed set of queries the module issues is
/// an enumeration: a caller cannot pass an arbitrary or misspelled name, and the set is declared once.
/// </remarks>
internal enum CollisionQuery
{
    ElementBoundingBox,
    ElementSolids,
    ConnectedElementIds,
    TargetElements,
    ModelCategoryFilter,
    Groups,
    SpatialIndex,
    PairIntersects,
    LinkGroups,
    LinkGroupsByInstance,
    LinkSpatialIndex,
    FamilyTreeRoots,
}

internal static class CollisionQueryExtensions
{
    private const string Prefix = "collision-diagnostics:";

    /// <summary>Gets the cache name of the query, prefixed with the owning module.</summary>
    /// <param name="query">The collision query.</param>
    /// <returns>The query name stored in the document query key.</returns>
    internal static string CacheName(this CollisionQuery query) => Prefix + query;
}
