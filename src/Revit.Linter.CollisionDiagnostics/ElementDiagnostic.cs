using Microsoft.Extensions.Logging;
using Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;
using Revit.Linter.CollisionDiagnostics.Infrastructure;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Extensions;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Spatial;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.Languages.Factories;
using Revit.Sugar;

namespace Revit.Linter.CollisionDiagnostics;

/// <summary>
/// Checks one element for volumetric collisions against the elements selected by <c>Take</c>.
/// </summary>
/// <remarks>
/// The check runs in four phases per target element:
/// <para><b>1. Selection.</b> Read the target solids; an element without solids is valid.
/// Resolve the target group from the partitioning by the <c>GroupBy</c> formula.</para>
/// <para><b>2. Broad phase.</b> Query the spatial index of that group and keep only the candidates
/// whose bounding boxes may overlap the target box, instead of scanning the whole group.</para>
/// <para><b>3. Narrow phase.</b> For each candidate except the target itself: exact bounding-box
/// overlap, then the solid Boolean intersection. A failed Boolean operation is treated as a
/// collision and logged.</para>
/// <para><b>4. Reporting.</b> Collect every intersecting element, sorted by identifier for a
/// deterministic report; no intersections means valid.</para>
/// <para>Solids, bounding boxes, groups, spatial indexes and pair intersections are all cached,
/// so the expensive steps run once per group and once per unordered element pair even though
/// every element is checked as a target in both directions.</para>
/// <para>When link checks are enabled, loaded Revit links contribute read-only candidates through
/// <see cref="LinkedModelCollisions"/>: their hits stay text-only in the report message without
/// entering the dependency list.</para>
/// <para>When nested-family skipping is enabled, a pair is skipped when both elements resolve to
/// one top-level family instance: a parent and its nested family, and nested families of one parent,
/// overlap by family design. The relation is read from the model, so it does not depend on the
/// rule's formulas or on whether the parent itself is selected.</para>
/// </remarks>
internal sealed class ElementDiagnostic(
    ElementFilterFactory elementFilterFactory,
    ElementFunctionFactory elementFunctionFactory,
    IGetElementBoundingBoxService getElementBoundingBox,
    IGetElementGeometryService getElementGeometry,
    IGetConnectedElementIdsService getConnectedElementIds,
    IDocumentQueryService documentQueries,
    ILogger<ElementDiagnostic> logger) : IElementDiagnostic
{
    public required ElementDiagnosticId Identity { get; init; }
    public required string TakeFormula { get; init; }
    public required string GroupByFormula { get; init; }
    public bool IncludeLinks { get; init; }
    /// <summary>Gets whether elements joined to the target by MEP connectors are skipped.</summary>
    public bool IgnoreConnectedElements { get; init; } = false;
    /// <summary>Gets whether elements that belong to one placed family are skipped.</summary>
    public bool IgnoreNestedFamilies { get; init; } = false;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        IReadOnlyCollection<Solid> targetSolids = getElementGeometry.Execute(targetElement, view);

        if (targetSolids.Count == 0) return DiagnosticFeedback.Valid;

        BoundingBoxXYZ targetBoundingBox = getElementBoundingBox.Execute(targetElement, view);
        ElementGroups groups = Groups.Get(document, view);
        string group = Groups.GroupOf(groups, targetElement);
        BoundingBoxGridIndex spatialIndex = GetSpatialIndex(document, view, groups, group);
        HashSet<long>? connectedIds = IgnoreConnectedElements
            ? getConnectedElementIds.Execute(targetElement)
            : null;
        FamilyTrees? familyTrees = IgnoreNestedFamilies
            ? documentQueries.GetCached(
                document, null, CollisionQuery.FamilyTreeRoots, static () => new FamilyTrees())
            : null;
        List<Element> intersections = FindIntersections(
            document, view, spatialIndex, targetElement, targetBoundingBox, targetSolids, connectedIds,
            familyTrees);
        List<LinkHit> linkHits = IncludeLinks
            ? Links.Find(document, view, targetElement, targetBoundingBox, targetSolids, group)
            : [];

        if (intersections.Count == 0 && linkHits.Count == 0) return DiagnosticFeedback.Valid;

        return CreateCollisionFeedback(intersections, linkHits);
    }

    private CollisionElementPool ElementPool => field ??= new(
        elementFilterFactory, elementFunctionFactory, documentQueries, TakeFormula, GroupByFormula);

    private PairIntersections Intersections => field ??= new(documentQueries, logger);

    private CollisionGroups Groups => field ??= new(ElementPool, documentQueries, logger, Identity);

    private LinkedModelCollisions Links => field ??= new(
        documentQueries, getElementBoundingBox, getElementGeometry, Intersections, ElementPool, logger);

    // Built once per (document, view, formulas, group) and reused by every target element's Execute
    // call for that group - see BoundingBoxGridIndex for why this replaces a linear scan of the group
    // (O(N) bounding-box comparisons per target -> O(N^2) total for the group).
    private BoundingBoxGridIndex GetSpatialIndex(
        Document document, View? view, ElementGroups groups, string group) =>
        documentQueries.GetCached(
            document, view, CollisionQuery.SpatialIndex,
            () => BoundingBoxGridIndex.Build(
                groups.ByGroup[group],
                element => getElementBoundingBox.Execute(element, view)),
            TakeFormula, GroupByFormula, group);

    // Every intersecting element is collected, not only the first one found: the finding then shows
    // the complete set of collisions of the target and does not depend on the order of candidates.
    private List<Element> FindIntersections(
        Document document,
        View? view,
        BoundingBoxGridIndex spatialIndex,
        Element targetElement,
        BoundingBoxXYZ targetBoundingBox,
        IReadOnlyCollection<Solid> targetSolids,
        HashSet<long>? connectedIds,
        FamilyTrees? familyTrees)
    {
        long targetElementId = targetElement.Id.IdValue();
        List<Element> intersections = [];

        foreach (Element element in spatialIndex.Query(targetBoundingBox))
        {
            long elementId = element.Id.IdValue();

            if (elementId == targetElementId) continue;

            // Joined elements overlap by family geometry at the joint; checking them wastes
            // the bounding-box, geometry and Boolean work below on a certain false positive.
            // Connector references are mutual, so the target set covers both check directions.
            if (connectedIds?.Contains(elementId) == true) continue;

            // A parent and its nested components, and nested components of one parent, belong to
            // one placed family: their overlap is decided inside the family, so the pair is
            // skipped even when the parent itself is not selected by the rule.
            if (familyTrees?.AreRelated(targetElement, element) == true) continue;

            BoundingBoxXYZ boundingBox = getElementBoundingBox.Execute(element, view);

            if (!boundingBox.Overlaps(targetBoundingBox)) continue;

            IReadOnlyCollection<Solid> solids = getElementGeometry.Execute(element, view);

            if (Intersections.Intersects(
                document, view, PairIntersections.HostPair(elementId, targetElementId),
                solids, targetSolids, elementId, targetElementId))
                intersections.Add(element);
        }

        return intersections;
    }

    private static DiagnosticFeedback CreateCollisionFeedback(
        List<Element> intersections, List<LinkHit> linkHits)
    {
        // Ordered by identifier so the message and the dependency list are the same on every run.
        intersections.Sort((first, second) => first.Id.IdValue().CompareTo(second.Id.IdValue()));
        linkHits.Sort((first, second) =>
        {
            int byLink = string.Compare(first.LinkName, second.LinkName, StringComparison.Ordinal);
            return byLink != 0 ? byLink : first.Id.IdValue().CompareTo(second.Id.IdValue());
        });

        // Linked hits stay text-only: they never enter the dependency list, so visualization and fix
        // pipelines keep working with host elements alone. Linked identifiers stay out of the clickable
        // element identifiers as well: a linked number resolved in the host document could point at
        // an unrelated host element, so only host identifiers become links.
        return new(DiagnosticVerdict.NotValid,
            new()
            {
                { "intersection.elementNames", string.Join(", ", intersections.Select(element => element.Name).Concat(linkHits.Select(hit => $"{hit.Name} [{hit.LinkName}]"))) },
                { "intersection.elementIds", intersections.Select(element => element.Id).ToArray() },
                { "intersection.count", intersections.Count + linkHits.Count },
            },
            [.. intersections]);
    }
}
