using System.Globalization;
using Microsoft.Extensions.Logging;
using Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Extensions;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Spatial;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Sugar;

namespace Revit.Linter.CollisionDiagnostics.Infrastructure;

/// <summary>The collision of a host target with one element of a loaded Revit link.</summary>
/// <remarks>Linked hits stay text-only in the report: only host elements enter the dependency list.</remarks>
internal sealed record LinkHit(string Name, ElementId Id, string LinkName);

/// <summary>The verified and unverified collisions of a host target with loaded Revit links.</summary>
/// <remarks>
/// An unverified hit is a bounding-box overlap whose solids could not be compared: Revit rejected the
/// Boolean operation, so the overlap is unknown and the report must not call it a collision.
/// </remarks>
internal sealed record LinkCollisions(List<LinkHit> Hits, List<LinkHit> Unverified);

/// <summary>Matches host targets against the elements of loaded Revit links.</summary>
/// <remarks>
/// Linked candidates are read-only and never act as targets, so link-versus-link pairs are never
/// checked and the report stays on host elements. Their bounding boxes are transformed into host
/// coordinates once, the group is partitioned by link instance once, and the target solids are moved
/// into link-local coordinates once per instance instead of moving every overlapping linked solid.
/// A link whose transform is not conformal is reported by bounding boxes only, without the solids
/// check, and a lower-level exception while reading the geometry skips that candidate with a log.
/// </remarks>
internal sealed class LinkedModelCollisions(
    IDocumentQueryService documentQueries,
    IGetElementBoundingBoxService getElementBoundingBox,
    IGetElementGeometryService getElementGeometry,
    PairIntersections pairIntersections,
    CollisionElementPool elementPool,
    ILogger logger)
{
    /// <summary>Finds the linked elements that collide with one host target.</summary>
    /// <param name="document">The host document.</param>
    /// <param name="view">The host view, used only for the pair-intersection cache key.</param>
    /// <param name="targetElement">The host target.</param>
    /// <param name="targetBoundingBox">The target bounding box in host coordinates.</param>
    /// <param name="targetSolids">The target solids in host coordinates.</param>
    /// <param name="group">The target group, whose linked candidates are compared.</param>
    /// <returns>The linked collisions, unordered, separated into verified and unverified hits.</returns>
    internal LinkCollisions Find(
        Document document,
        View? view,
        Element targetElement,
        BoundingBoxXYZ targetBoundingBox,
        IReadOnlyCollection<Solid> targetSolids,
        string group)
    {
        // Connected pairs are never filtered here: connectors join elements only within one
        // document, so a host target cannot be joined to a linked candidate, and element
        // identifiers repeat across documents, so the host connected set must never be
        // compared against linked identifiers.
        List<LinkHit> hits = [];
        List<LinkHit> unverified = [];
        long targetElementId = targetElement.Id.IdValue();

        foreach (LinkInstancePartition partition in GetPartitions(document, group))
        {
            (BoundingBoxGridIndex index, Dictionary<long, LinkedCandidate> byElementId) =
                GetSpatialIndex(document, group, partition);

            // Bounding-box-overlapping candidates of this instance first: the target transform below
            // is per instance, so it runs once even when several candidates overlap the target.
            List<LinkedCandidate> overlapping = [];
            foreach (Element element in index.Query(targetBoundingBox))
            {
                LinkedCandidate candidate = byElementId[element.Id.IdValue()];
                if (candidate.HostBoundingBox.Overlaps(targetBoundingBox))
                    overlapping.Add(candidate);
            }

            if (overlapping.Count == 0) continue;

            if (!partition.SolidsTestable)
            {
                hits.AddRange(overlapping.Select(candidate =>
                    new LinkHit(candidate.Element.Name, candidate.Element.Id, partition.LinkName)));
                continue;
            }

            IReadOnlyCollection<Solid> targetInLink = GetTargetSolidsInLink(targetSolids, partition.Transform);

            // The check feeds two result lists, so the loop stays explicit: a LINQ pass would either
            // run the cached pair check twice per candidate or materialize an intermediate list.
#pragma warning disable S3267 // SonarAnalyzer: loops should be simplified with LINQ.
            foreach (LinkedCandidate candidate in overlapping)
            {
                long elementId = candidate.Element.Id.IdValue();
                PairIntersectionResult result = pairIntersections.Intersects(
                    document, view, PairIntersections.LinkPair(partition.InstanceId, elementId, targetElementId),
                    getElementGeometry.Execute(candidate.Element, null), targetInLink, elementId, targetElementId);

                if (result == PairIntersectionResult.None) continue;

                LinkHit hit = new(candidate.Element.Name, candidate.Element.Id, partition.LinkName);
                if (result == PairIntersectionResult.Intersects)
                    hits.Add(hit);
                else
                    unverified.Add(hit);
            }
#pragma warning restore S3267
        }

        return new LinkCollisions(hits, unverified);
    }

    // Candidates of one group partitioned by link instance: element identifiers are unique only
    // inside one document, so one spatial index and one identifier map serve one instance. Built
    // once per run: every candidate of one instance shares its transform, link name and
    // testability, and the grouping formula would otherwise run once per target of the group.
    private IReadOnlyList<LinkInstancePartition> GetPartitions(Document document, string group) =>
        documentQueries.GetCached(
            document, null, CollisionQuery.LinkGroupsByInstance,
            () => (IReadOnlyList<LinkInstancePartition>)
                [
                    .. GetGroups(document)[group]
                        .GroupBy(candidate => candidate.LinkInstance.Id.IdValue())
                        .Select(candidates => new LinkInstancePartition(
                            candidates.Key,
                            candidates.First().Transform,
                            candidates.First().LinkName,
                            candidates.First().SolidsTestable,
                            [.. candidates])),
                ],
            elementPool.TakeFormula, elementPool.GroupByFormula, group);

    // Partitioned once per run: the grouping formula runs once per linked candidate, and candidates
    // without readable geometry are dropped here rather than failing the index build later.
    private ILookup<string, LinkedCandidate> GetGroups(Document document) =>
        documentQueries.GetCached(
            document, null, CollisionQuery.LinkGroups,
            () => CollectCandidates(document).ToLookup(candidate => elementPool.GroupOf(candidate.Element)),
            elementPool.TakeFormula, elementPool.GroupByFormula);

    // The index and the element map are cached together because both derive from the same candidate
    // list of one instance and one group.
    private (BoundingBoxGridIndex Index, Dictionary<long, LinkedCandidate> ByElementId) GetSpatialIndex(
        Document document, string group, LinkInstancePartition partition) =>
        documentQueries.GetCached(
            document, null, CollisionQuery.LinkSpatialIndex,
            () =>
            {
                Dictionary<long, LinkedCandidate> byElementId =
                    partition.Candidates.ToDictionary(candidate => candidate.Element.Id.IdValue());
                BoundingBoxGridIndex index = BoundingBoxGridIndex.Build(
                    partition.Candidates.Select(candidate => candidate.Element),
                    element => byElementId[element.Id.IdValue()].HostBoundingBox);
                return (index, byElementId);
            },
            elementPool.TakeFormula, elementPool.GroupByFormula, group,
            partition.InstanceId.ToString(CultureInfo.InvariantCulture));

    private List<LinkedCandidate> CollectCandidates(Document document)
    {
        List<LinkedCandidate> candidates = [];

        // Link instances are the same for every collision rule in the run: they come from the
        // shared class query instead of a dedicated collector per groups miss.
        foreach (RevitLinkInstance instance in documentQueries.GetElementsOfClass<RevitLinkInstance>(document))
        {
            Document? linkDocument = instance.GetLinkDocument();
            if (linkDocument is null)
            {
                logger.LogWarning(
                    "Revit link {LinkName} ({LinkId}) is not loaded; skipping it for collisions.",
                    instance.Name,
                    instance.Id.IdValue());
                continue;
            }

            Transform transform = instance.GetTotalTransform();
            bool solidsTestable = transform.IsConformal;
            if (!solidsTestable)
            {
                logger.LogWarning(
                    "Revit link {LinkName} ({LinkId}) has a non-conformal transform; its collisions are reported without a solids check.",
                    instance.Name,
                    instance.Id.IdValue());
            }

            foreach (Element element in elementPool.Collect(linkDocument, null))
            {
                BoundingBoxXYZ hostBoundingBox;
                try
                {
                    hostBoundingBox = getElementBoundingBox.Execute(element, null).Transformed(transform);
                }
                catch (InvalidOperationException exception)
                {
                    logger.LogDebug(
                        exception,
                        "Skipping linked collision candidate {ElementId} with unreadable geometry",
                        element.Id.IdValue());
                    continue;
                }

                candidates.Add(new(instance, element, linkDocument.Title, hostBoundingBox, transform, solidsTestable));
            }
        }

        return candidates;
    }

    // The target solids moved once into the link-local coordinates of one instance, instead of
    // moving every overlapping linked solid into host coordinates. Both directions report the same
    // verdict: the instance transform is conformal, so it preserves intersection volumes (up to a
    // uniform scale factor absorbed by the shared Epsilon). The moved copies live only for this
    // target and instance: unlike the linked solids they replace, they are never retained in the
    // transaction cache, while the native linked solids still come from the shared geometry cache.
    private static List<Solid> GetTargetSolidsInLink(
        IReadOnlyCollection<Solid> targetSolids, Transform transform)
    {
        Transform inverse = transform.Inverse;
        List<Solid> moved = new(targetSolids.Count);
        moved.AddRange(targetSolids.Select(solid => SolidUtils.CreateTransformed(solid, inverse)));
        return moved;
    }

    // A collision candidate from a linked document. The bounding box is stored in host coordinates,
    // so later stages never touch link coordinates; solids are transformed lazily because only
    // bounding-box-overlapping pairs need them.
    private sealed record LinkedCandidate(
        RevitLinkInstance LinkInstance, Element Element, string LinkName, BoundingBoxXYZ HostBoundingBox,
        Transform Transform, bool SolidsTestable);

    // Candidates of one group that belong to one link instance. Every candidate of an instance
    // shares its transform, link name and testability, so they are stored once per partition.
    private sealed record LinkInstancePartition(
        long InstanceId, Transform Transform, string LinkName, bool SolidsTestable,
        IReadOnlyList<LinkedCandidate> Candidates);
}
