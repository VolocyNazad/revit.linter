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
/// overlap, then the solid Boolean intersection. A rejected Boolean operation does not make the pair a
/// collision, because Revit also rejects it for coincident faces and edges of elements that only touch;
/// such a pair is kept as unverified.</para>
/// <para><b>4. Reporting.</b> Collect every intersecting element, sorted by identifier for a
/// deterministic report; no intersecting or unverified elements means valid. A target whose only
/// candidates are unverified is reported with its own message, which says that the overlap could not
/// be determined instead of claiming a collision.</para>
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
    /// <summary>Gets the replaceable volume calculation of the solid check, used by tests only.</summary>
    /// <remarks>
    /// Revit rejects the intersection of touching geometry only for some real models, so a test cannot
    /// obtain that rejection deterministically; <see langword="null"/> from the calculation stands for
    /// a rejected operation and lets the test simulate it.
    /// </remarks>
    internal Func<Solid, Solid, double?>? IntersectionVolume { get; init; }

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
        Collisions collisions = FindIntersections(
            document, view, spatialIndex, targetElement, targetBoundingBox, targetSolids, connectedIds,
            familyTrees);
        LinkCollisions links = IncludeLinks
            ? Links.Find(document, view, targetElement, targetBoundingBox, targetSolids, group)
            : new LinkCollisions([], []);

        // A verified collision wins: the finding is a real overlap even when other pairs of the same
        // target could not be compared, and those pairs stay visible in the log only.
        if (collisions.Hits.Count > 0 || links.Hits.Count > 0)
            return CreateCollisionFeedback(collisions.Hits, links.Hits);

        if (collisions.Unverified.Count == 0 && links.Unverified.Count == 0)
            return DiagnosticFeedback.Valid;

        return CreateUnverifiedFeedback(collisions.Unverified, links.Unverified);
    }

    private CollisionElementPool ElementPool => field ??= new(
        elementFilterFactory, elementFunctionFactory, documentQueries, TakeFormula, GroupByFormula);

    private PairIntersections Intersections => field ??= new(documentQueries, logger, IntersectionVolume);

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
    // A candidate whose solids could not be compared is collected apart from the intersecting ones,
    // because the report must not present it as a collision.
    private Collisions FindIntersections(
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
        List<Element> unverified = [];

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

            PairIntersectionResult result = Intersections.Intersects(
                document, view, PairIntersections.HostPair(elementId, targetElementId),
                solids, targetSolids, elementId, targetElementId);

            if (result == PairIntersectionResult.Intersects)
                intersections.Add(element);
            else if (result == PairIntersectionResult.Unverified)
                unverified.Add(element);
        }

        return new Collisions(intersections, unverified);
    }

    private static DiagnosticFeedback CreateCollisionFeedback(
        List<Element> intersections, List<LinkHit> linkHits)
    {
        Sort(intersections, linkHits);

        // Linked hits stay text-only: they never enter the dependency list, so visualization and fix
        // pipelines keep working with host elements alone. Linked identifiers stay out of the clickable
        // element identifiers as well: a linked number resolved in the host document could point at
        // an unrelated host element, so only host identifiers become links.
        return new(
            DiagnosticVerdict.NotValid,
            CreateIntersectionArguments(intersections, linkHits),
            [.. intersections]);
    }

    // A pair whose solids could not be compared is reported as unverified rather than as a collision:
    // the rule's own message would claim an overlap that the check never established. The wording
    // explains the reason in user terms, and the listed elements are exactly the pairs to review.
    private static DiagnosticFeedback CreateUnverifiedFeedback(
        List<Element> unverifiedElements, List<LinkHit> unverifiedLinks)
    {
        Sort(unverifiedElements, unverifiedLinks);

        return new(
            DiagnosticVerdict.NotValid,
            CreateIntersectionArguments(unverifiedElements, unverifiedLinks),
            [.. unverifiedElements])
        {
            MessageFormat = CollisionDiagnosticLocalizations.GetString("Unverified_message"),
        };
    }

    // Ordered by identifier so the message and the dependency list are the same on every run.
    private static void Sort(List<Element> elements, List<LinkHit> linkHits)
    {
        elements.Sort((first, second) => first.Id.IdValue().CompareTo(second.Id.IdValue()));
        linkHits.Sort((first, second) =>
        {
            int byLink = string.Compare(first.LinkName, second.LinkName, StringComparison.Ordinal);
            return byLink != 0 ? byLink : first.Id.IdValue().CompareTo(second.Id.IdValue());
        });
    }

    private static Dictionary<string, object> CreateIntersectionArguments(
        List<Element> elements, List<LinkHit> linkHits) => new()
        {
            { "intersection.elementNames", string.Join(", ", elements.Select(element => element.Name).Concat(linkHits.Select(hit => $"{hit.Name} [{hit.LinkName}]"))) },
            { "intersection.elementIds", elements.Select(element => element.Id).ToArray() },
            { "intersection.count", elements.Count + linkHits.Count },
        };

    // One target's narrow-phase result: the pairs that really overlap and the pairs whose overlap
    // could not be determined.
    private sealed record Collisions(List<Element> Hits, List<Element> Unverified);
}
