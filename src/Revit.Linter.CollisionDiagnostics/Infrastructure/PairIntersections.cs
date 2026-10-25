using Microsoft.Extensions.Logging;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Extensions;
using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.CollisionDiagnostics.Infrastructure;

/// <summary>
/// Computes and caches the exact solid intersection of an element pair.
/// </summary>
/// <remarks>
/// The diagnostic checks every element as a target, so the unordered pair is met twice: once in each
/// direction. The result depends only on the two elements' solids, not on the rule, so every collision
/// rule that meets the same pair in the same document and view reuses the cached answer. A failed
/// Boolean operation is treated as a collision and logged once.
/// </remarks>
internal sealed class PairIntersections(IDocumentQueryService documentQueries, ILogger logger)
{
    private const double Epsilon = 1e-6;

    /// <summary>Gets the cache key of a host pair, canonicalized by identifier order.</summary>
    internal static string HostPair(long elementId, long targetElementId) =>
        elementId < targetElementId
            ? $"{elementId}:{targetElementId}"
            : $"{targetElementId}:{elementId}";

    /// <summary>Gets the cache key of a host-versus-link pair.</summary>
    /// <remarks>
    /// Element identifiers repeat across documents, so the key carries which document side each
    /// identifier comes from. Unlike host pairs, the order is fixed instead of canonicalized: linked
    /// elements never act as targets, so every rule builds the same key for the pair.
    /// </remarks>
    internal static string LinkPair(long instanceId, long elementId, long targetElementId) =>
        $"L{instanceId}:{elementId}|H:{targetElementId}";

    /// <summary>Gets whether the two solids collections intersect each other.</summary>
    /// <param name="document">The document the pair belongs to.</param>
    /// <param name="view">The view the geometry was computed for, or <see langword="null"/> for default geometry.</param>
    /// <param name="pairKey">The pair identifier from <see cref="HostPair"/> or <see cref="LinkPair"/>.</param>
    /// <param name="solids">The candidate element's solids.</param>
    /// <param name="targetSolids">The target element's solids, already in the candidate's coordinates.</param>
    /// <param name="elementId">The candidate identifier, used only for the failure log.</param>
    /// <param name="targetElementId">The target identifier, used only for the failure log.</param>
    internal bool Intersects(
        Document document,
        View? view,
        string pairKey,
        IReadOnlyCollection<Solid> solids,
        IReadOnlyCollection<Solid> targetSolids,
        long elementId,
        long targetElementId) =>
        documentQueries.GetCached(
            document, view, CollisionQueryNames.PairIntersects,
            () => Intersects(solids, targetSolids, elementId, targetElementId),
            pairKey);

    // Inputs are already non-degenerate: ElementGeometryExtensions.CollectSolids keeps only solids
    // with Volume greater than Epsilon, so no zero-volume guard is needed here. The result volume
    // is the intersection test itself - ExecuteBooleanOperation returns a solid, not a boolean -
    // and Epsilon additionally filters out touches and slivers.
    private bool Intersects(
        IEnumerable<Solid> solids,
        IEnumerable<Solid> targetSolids,
        long elementId,
        long targetElementId)
    {
        foreach (Solid solid in solids)
        {
            foreach (Solid targetSolid in targetSolids)
            {
                bool booleanOperationFailed = false;
                try
                {
                    if (BooleanOperationsUtils.ExecuteBooleanOperation(
                        targetSolid, solid, BooleanOperationsType.Intersect).Volume > Epsilon) return true;
                }
                catch (Autodesk.Revit.Exceptions.InvalidOperationException)
                {
                    booleanOperationFailed = true;
                }

                if (!booleanOperationFailed) continue;

                logger.LogWarning(
                    "Boolean intersection failed for elements {ElementId} and {TargetElementId}; treating the pair as a potential collision.",
                    elementId,
                    targetElementId);
                return true;
            }
        }
        return false;
    }
}
