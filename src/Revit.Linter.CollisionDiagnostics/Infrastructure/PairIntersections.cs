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
/// rule that meets the same pair in the same document and view reuses the cached answer. A rejected
/// Boolean operation yields <see cref="PairIntersectionResult.Unverified"/>: it is not an overlap, so
/// the pair is never reported as a collision on the rejection alone. The rejection is logged at Debug
/// with the exception and the pair outcome once at Warning. The volume calculation is replaceable only
/// so a test can simulate that rejection: Revit does not reject the geometry of touching elements on
/// every model or version.
/// </remarks>
internal sealed class PairIntersections(
    IDocumentQueryService documentQueries,
    ILogger logger,
    Func<Solid, Solid, double?>? intersectionVolume = null)
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
    /// <param name="elementId">The candidate identifier, used only for the rejection log.</param>
    /// <param name="targetElementId">The target identifier, used only for the rejection log.</param>
    /// <returns>The check result; <see cref="PairIntersectionResult.Unverified"/> when Revit rejects the Boolean operation.</returns>
    internal PairIntersectionResult Intersects(
        Document document,
        View? view,
        string pairKey,
        IReadOnlyCollection<Solid> solids,
        IReadOnlyCollection<Solid> targetSolids,
        long elementId,
        long targetElementId) =>
        documentQueries.GetCached(
            document, view, CollisionQuery.PairIntersects,
            () => Intersects(solids, targetSolids, elementId, targetElementId),
            pairKey);

    // Inputs are already non-degenerate: ElementGeometryExtensions.CollectSolids keeps only solids
    // with Volume greater than Epsilon, so no zero-volume guard is needed here. The result volume
    // is the intersection test itself - ExecuteBooleanOperation returns a solid, not a boolean -
    // and Epsilon additionally filters out touches and slivers.
    //
    // A rejected Boolean operation is not evidence of an overlap: Revit also rejects it for coincident
    // faces and edges, which is exactly the geometry of elements that only touch. One rejected solid
    // pair says nothing about the others either, so the remaining pairs are still checked, and the pair
    // becomes a collision only on a computed intersection volume. The rejection leaves the pair
    // unverified for the report.
    private PairIntersectionResult Intersects(
        IEnumerable<Solid> solids,
        IEnumerable<Solid> targetSolids,
        long elementId,
        long targetElementId)
    {
        bool intersectionFailed = false;

        foreach (Solid solid in solids)
        {
            foreach (Solid targetSolid in targetSolids)
            {
                double? volume = GetIntersectionVolume(targetSolid, solid);
                if (volume is null)
                {
                    // One rejected pair of solids does not make the other pairs uncomputable; the
                    // outcome is logged once per element pair, not once per solid pair.
                    if (intersectionFailed) continue;

                    intersectionFailed = true;
                    logger.LogWarning(
                        "The solid intersection of elements {ElementId} and {TargetElementId} could not be computed; " +
                        "the pair is reported as unverified.",
                        elementId,
                        targetElementId);
                    continue;
                }

                if (volume > Epsilon) return PairIntersectionResult.Intersects;
            }
        }
        return intersectionFailed ? PairIntersectionResult.Unverified : PairIntersectionResult.None;
    }

    // The volume calculation is replaceable only for tests: production asks Revit, while a test
    // substitutes a rejection it cannot obtain from a real model deterministically. A rejected
    // operation returns null, because the exception type Revit throws has no public constructor and
    // carries no detail beyond a generic message.
    private double? GetIntersectionVolume(Solid targetSolid, Solid solid)
    {
        if (intersectionVolume is not null) return intersectionVolume(targetSolid, solid);

        try
        {
            return BooleanOperationsUtils.ExecuteBooleanOperation(
                targetSolid, solid, BooleanOperationsType.Intersect).Volume;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException exception)
        {
            logger.LogDebug(exception, "The Boolean intersection of two solids failed");
            return null;
        }
    }
}
