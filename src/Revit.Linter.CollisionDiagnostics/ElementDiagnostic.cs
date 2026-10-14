using Microsoft.Extensions.Logging;
using Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;
using Revit.Linter.CollisionDiagnostics.Infrastructure;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Extensions;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Spatial;
using Revit.Linter.DocumentQueries.Abstractions.Models;
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
/// <para>When link checks are enabled, loaded Revit links contribute read-only candidates:
/// their bounding boxes are transformed into host coordinates once, the group is partitioned
/// by link instance once, and the target solids are moved into link-local coordinates once per
/// instance instead of moving every overlapping linked solid. Pair keys carry
/// which document side each identifier comes from, and linked hits stay text-only in the
/// report message without entering the dependency list.</para>
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
    private const double Epsilon = 1e-6;

    // Separates the parts of a composite cache argument; it cannot occur in a formula or a group value
    // typed by a user, so two different part lists never produce the same argument.
    private const char ArgumentSeparator = '\u001F';

    public required ElementDiagnosticId Identity { get; init; }
    public required string TakeFormula { get; init; }
    public required string GroupByFormula { get; init; }
    public bool IncludeLinks { get; init; }
    /// <summary>Gets whether elements joined to the target by MEP connectors are skipped.</summary>
    public bool IgnoreConnectedElements { get; init; } = false;
    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        IReadOnlyCollection<Solid> targetSolids = getElementGeometry.Execute(targetElement, view);

        if (targetSolids.Count == 0) return DiagnosticFeedback.Valid;

        BoundingBoxXYZ targetBoundingBox = getElementBoundingBox.Execute(targetElement, view);
        ElementGroups groups = GetGroups(document, view);
        string group = ResolveTargetGroup(groups, targetElement);
        BoundingBoxGridIndex spatialIndex = GetSpatialIndex(document, view, groups, group);
        HashSet<long>? connectedIds = IgnoreConnectedElements
            ? getConnectedElementIds.Execute(targetElement)
            : null;
        List<Element> intersections = FindIntersections(
            document, view, spatialIndex, targetElement, targetBoundingBox, targetSolids, connectedIds);
        List<LinkHit> linkHits = FindLinkIntersections(
            document, view, targetElement, targetBoundingBox, targetSolids, group);

        if (intersections.Count == 0 && linkHits.Count == 0) return DiagnosticFeedback.Valid;

        return CreateCollisionFeedback(intersections, linkHits);
    }

    // The group of an element was already computed when the groups were built, so the formula is
    // evaluated again only for a target outside the collected elements.
    private string ResolveTargetGroup(ElementGroups groups, Element targetElement)
    {
        return groups.GroupByElementId.TryGetValue(targetElement.Id.Value(), out string? knownGroup)
            ? knownGroup
            : GetGroup(targetElement);
    }

    // Built once per (document, view, formulas, group) and reused by every target element's Execute
    // call for that group - see BoundingBoxGridIndex for why this replaces a linear scan of the group
    // (O(N) bounding-box comparisons per target -> O(N^2) total for the group).
    private BoundingBoxGridIndex GetSpatialIndex(Document document, View? view, ElementGroups groups, string group)
    {
        return documentQueries.GetOrCreate(
            DocumentQueryKey.Create(
                document, CollisionQueryNames.SpatialIndex, view,
                argument: $"{TakeFormula}{ArgumentSeparator}{GroupByFormula}{ArgumentSeparator}{group}"),
            () => BoundingBoxGridIndex.Build(
                groups.ByGroup[group],
                element => getElementBoundingBox.Execute(element, view)));
    }

    // Every intersecting element is collected, not only the first one found: the finding then shows
    // the complete set of collisions of the target and does not depend on the order of candidates.
    private List<Element> FindIntersections(
        Document document,
        View? view,
        BoundingBoxGridIndex spatialIndex,
        Element targetElement,
        BoundingBoxXYZ targetBoundingBox,
        IReadOnlyCollection<Solid> targetSolids,
        HashSet<long>? connectedIds)
    {
        long targetElementId = targetElement.Id.Value();
        List<Element> intersections = [];

        foreach (Element element in spatialIndex.Query(targetBoundingBox))
        {
            long elementId = element.Id.Value();

            if (elementId == targetElementId) continue;

            // Joined elements overlap by family geometry at the joint; checking them wastes
            // the bounding-box, geometry and Boolean work below on a certain false positive.
            // Connector references are mutual, so the target set covers both check directions.
            if (connectedIds?.Contains(elementId) == true) continue;

            BoundingBoxXYZ boundingBox = getElementBoundingBox.Execute(element, view);

            if (!boundingBox.Overlaps(targetBoundingBox)) continue;

            IReadOnlyCollection<Solid> solids = getElementGeometry.Execute(element, view);

            (long minId, long maxId) = elementId < targetElementId
                ? (elementId, targetElementId)
                : (targetElementId, elementId);

            if (HasIntersectionCached(
                document, view, $"{minId}:{maxId}", elementId, targetElementId, solids, targetSolids))
                intersections.Add(element);
        }

        return intersections;
    }

    private static DiagnosticFeedback CreateCollisionFeedback(List<Element> intersections, List<LinkHit> linkHits)
    {
        // Ordered by identifier so the message and the dependency list are the same on every run.
        intersections.Sort((first, second) => first.Id.Value().CompareTo(second.Id.Value()));
        linkHits.Sort((first, second) =>
        {
            int byLink = string.Compare(first.LinkName, second.LinkName, StringComparison.Ordinal);
            return byLink != 0 ? byLink : first.Id.Value().CompareTo(second.Id.Value());
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

    // A collision candidate from a linked document. The bounding box is stored in host coordinates,
    // so later stages never touch link coordinates; solids are transformed lazily because only
    // bounding-box-overlapping pairs need them.
    private sealed record LinkedCandidate(
        RevitLinkInstance LinkInstance, Element Element, Transform Transform, string LinkName,
        BoundingBoxXYZ HostBoundingBox, bool SolidsTestable);

    // A found collision with a linked element, carried only in the report message.
    private sealed record LinkHit(string Name, ElementId Id, string LinkName);

    private List<LinkHit> FindLinkIntersections(
        Document document,
        View? view,
        Element targetElement,
        BoundingBoxXYZ targetBoundingBox,
        IReadOnlyCollection<Solid> targetSolids,
        string group)
    {
        List<LinkHit> hits = [];
        if (!IncludeLinks) return hits;

        // Connected pairs are never filtered here: connectors join elements only within one
        // document, so a host target cannot be joined to a linked candidate, and element
        // identifiers repeat across documents, so the host connected set must never be
        // compared against linked identifiers.

        long targetElementId = targetElement.Id.Value();
        // No deconstruction: KeyValuePair's Deconstruct is unavailable on net48, and the
        // identifier widens from int to long between Revit versions, so the pair is read
        // through Key and Value with an explicitly long identifier below.
        foreach (KeyValuePair<long, IReadOnlyList<LinkedCandidate>> instanceCandidates in GetLinkedCandidatesByInstance(document, group))
        {
            long instanceId = instanceCandidates.Key;
            IReadOnlyList<LinkedCandidate> candidates = instanceCandidates.Value;
            (BoundingBoxGridIndex index, Dictionary<long, LinkedCandidate> byElementId) =
                GetLinkSpatialIndex(document, group, instanceId, candidates);

            // Bounding-box-overlapping candidates of this instance first: the target transform below
            // is per instance, so it runs once even when several candidates overlap the target.
            List<LinkedCandidate> overlapping = [];
            foreach (Element element in index.Query(targetBoundingBox))
            {
                LinkedCandidate candidate = byElementId[element.Id.Value()];
                if (candidate.HostBoundingBox.Overlaps(targetBoundingBox))
                    overlapping.Add(candidate);
            }

            if (overlapping.Count == 0) continue;

            if (!candidates[0].SolidsTestable)
            {
                foreach (LinkedCandidate candidate in overlapping)
                    hits.Add(new(candidate.Element.Name, candidate.Element.Id, candidate.LinkName));
                continue;
            }

            IReadOnlyCollection<Solid> targetInLink = GetTargetSolidsInLink(targetSolids, candidates[0].Transform);

            foreach (LinkedCandidate candidate in overlapping)
            {
                long elementId = candidate.Element.Id.Value();
                IReadOnlyCollection<Solid> solids = getElementGeometry.Execute(candidate.Element, null);

                if (HasIntersectionCached(
                    document, view, LinkPairArgument(instanceId, elementId, targetElementId),
                    elementId, targetElementId, solids, targetInLink))
                    hits.Add(new(candidate.Element.Name, candidate.Element.Id, candidate.LinkName));
            }
        }

        return hits;
    }

    // Linked candidates of one group, partitioned by link instance: element identifiers are unique
    // only inside one document, so one spatial index and one identifier map serve one instance.
    // The index and the map are cached together because both derive from the same candidate list.
    private (BoundingBoxGridIndex Index, Dictionary<long, LinkedCandidate> ByElementId) GetLinkSpatialIndex(
        Document document, string group, long instanceId, IReadOnlyList<LinkedCandidate> candidates)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(
                document, CollisionQueryNames.LinkSpatialIndex,
                argument: $"{TakeFormula}{ArgumentSeparator}{GroupByFormula}{ArgumentSeparator}{group}{ArgumentSeparator}{instanceId}"),
            () =>
            {
                Dictionary<long, LinkedCandidate> byElementId =
                    candidates.ToDictionary(candidate => candidate.Element.Id.IdValue());
                BoundingBoxGridIndex index = BoundingBoxGridIndex.Build(
                    candidates.Select(candidate => candidate.Element),
                    element => byElementId[element.Id.Value()].HostBoundingBox);
                return (index, byElementId);
            });

    // Element identifiers repeat across documents, so a pair key carries which document side each
    // identifier comes from: H for the host target, L<instance> for the linked candidate. Unlike
    // host pairs, the order is fixed instead of canonicalized: linked elements never act as targets,
    // so a pair is always met in this one direction and every rule builds the same key.
    private static string LinkPairArgument(long instanceId, long elementId, long targetElementId)
        => $"L{instanceId}:{elementId}|H:{targetElementId}";

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

    // The group partitioned by link instance, built once per run: every candidate of one instance
    // shares its transform, link name and testability, and element identifiers repeat across
    // documents, so one partition serves one spatial index. Partitioning per target element instead
    // would repeat the same GroupBy for every target of the group.
    private IReadOnlyDictionary<long, IReadOnlyList<LinkedCandidate>> GetLinkedCandidatesByInstance(
        Document document, string group)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(
                document, CollisionQueryNames.LinkGroupsByInstance,
                argument: $"{TakeFormula}{ArgumentSeparator}{GroupByFormula}{ArgumentSeparator}{group}"),
            () => (IReadOnlyDictionary<long, IReadOnlyList<LinkedCandidate>>)GetLinkedGroups(document)[group]
                // The helper pins the key to long: Sugar's Value() widens from int to long
                // between Revit versions, and inference alone would follow the version.
                .GroupBy(candidate => candidate.LinkInstance.Id.IdValue())
                .ToDictionary(
                    partition => partition.Key,
                    partition => (IReadOnlyList<LinkedCandidate>)[.. partition]));

    // Partitioned once per run: the grouping formula runs once per linked candidate, and candidates
    // without readable geometry are dropped here rather than failing the index build later.
    private ILookup<string, LinkedCandidate> GetLinkedGroups(Document document)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(
                document, CollisionQueryNames.LinkGroups,
                argument: $"{TakeFormula}{ArgumentSeparator}{GroupByFormula}"),
            () =>
            {
                List<LinkedCandidate> placed = [];
                foreach ((RevitLinkInstance instance, Element element, Transform transform, string linkName, bool testable)
                    in CollectLinkedCandidates(document))
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
                            element.Id.Value());
                        continue;
                    }

                    placed.Add(new(instance, element, transform, linkName, hostBoundingBox, testable));
                }

                return placed.ToLookup(candidate => GetGroup(candidate.Element));
            });

    private List<(RevitLinkInstance Instance, Element Element, Transform Transform, string LinkName, bool SolidsTestable)> CollectLinkedCandidates(
        Document document)
    {
        List<(RevitLinkInstance, Element, Transform, string, bool)> candidates = [];
        if (!IncludeLinks) return candidates;

        List<RevitLinkInstance> instances = new FilteredElementCollector(document)
            .OfClass(typeof(RevitLinkInstance))
            .Cast<RevitLinkInstance>()
            .ToList();

        foreach (RevitLinkInstance instance in instances)
        {
            Document? linkDocument = instance.GetLinkDocument();
            if (linkDocument is null)
            {
                logger.LogWarning(
                    "Revit link {LinkName} ({LinkId}) is not loaded; skipping it for collisions.",
                    instance.Name,
                    instance.Id.Value());
                continue;
            }

            Transform transform = instance.GetTotalTransform();
            bool testable = transform.IsConformal;
            if (!testable)
            {
                logger.LogWarning(
                    "Revit link {LinkName} ({LinkId}) has a non-conformal transform; its collisions are reported without a solids check.",
                    instance.Name,
                    instance.Id.Value());
            }

            foreach (Element element in GetLinkElements(linkDocument))
                candidates.Add((instance, element, transform, linkDocument.Title, testable));
        }

        return candidates;
    }

    // A host view never applies inside a linked document, so linked elements are always collected
    // document-wide with default geometry options. This reuses the same filters as the host pool.
    private IList<Element> GetLinkElements(Document linkDocument) => GetElements(linkDocument, null);

    // The unordered pair (elementId, targetElementId) is evaluated from both directions across
    // the outer per-element loop in DiagnosticService: once with this element as the target, once
    // with the other element as the target. Without caching, the expensive Boolean solid
    // intersection would be executed twice for every colliding (or bounding-box-overlapping) pair.
    // Caching the result per unordered pair keeps both directions' reports intact (each element
    // still gets its own diagnostic feedback listing all of its collisions) while computing the
    // intersection only once.
    //
    // The result depends only on the two elements' solids, not on the rule, so every collision rule
    // that meets the same pair in the same document and view reuses it.
    private bool HasIntersectionCached(
        Document document,
        View? view,
        string pairArgument,
        long elementId,
        long targetElementId,
        IReadOnlyCollection<Solid> solids,
        IReadOnlyCollection<Solid> targetSolids)
    {
        return documentQueries.GetOrCreate(
            DocumentQueryKey.Create(
                document, CollisionQueryNames.PairIntersects, view, argument: pairArgument),
            () => HasIntersection(solids, targetSolids, elementId, targetElementId));
    }

    private ElementFilter Filter => field ??= elementFilterFactory.Create(TakeFormula);
    private Func<Element, object> GroupByDelegate => field ??= elementFunctionFactory.Create<object>(GroupByFormula, string.Empty);

    private string GetGroup(Element element) => GroupByDelegate.Invoke(element)?.ToString() ?? string.Empty;

    // The keys carry the formulas rather than the rule code: rules with the same formulas share the
    // collected elements and groups, and rules with different formulas can never read each other's.
    private ElementGroups GetGroups(Document document, View? view)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(
                document, CollisionQueryNames.Groups, view,
                argument: $"{TakeFormula}{ArgumentSeparator}{GroupByFormula}"),
            () => BuildGroups(GetCachedTargetElements(document, view), view));

    private ElementGroups BuildGroups(IList<Element> elements, View? view)
    {
        // Elements without geometry cannot intersect anything and their bounding box cannot be
        // built, so they stay out of every group instead of failing the spatial index below.
        // GetGeometry is cached, and the bounding-box service derives from the same cached geometry.
        List<Element> testable = elements
            .Where(element => documentQueries.GetGeometry(element, view) is not null)
            .ToList();
        if (testable.Count < elements.Count)
        {
            // Logged once per rule run: groups are built once per run through the query cache.
            // Such elements cannot collide, but selecting them still costs a geometry read each,
            // so narrowing the take filters to the reviewed categories speeds the check up.
            logger.LogWarning(
                "Collision diagnostic {DiagnosticCode} skipped {SkippedElementCount} selected elements " +
                "without geometry; they cannot collide. Narrow the take filters to the reviewed categories " +
                "to avoid selecting them.",
                Identity.Code, elements.Count - testable.Count);
        }
        Dictionary<long, string> groupByElementId = new(testable.Count);
        ILookup<string, Element> byGroup = testable.ToLookup(element =>
        {
            string group = GetGroup(element);
            groupByElementId[element.Id.Value()] = group;
            return group;
        });
        return new ElementGroups(byGroup, groupByElementId);
    }

    /// <summary>
    /// The collected elements partitioned by the <c>GroupBy</c> formula, indexed both ways:
    /// <c>ByGroup</c> answers "which elements are in this group" (keyed by group name),
    /// <c>GroupByElementId</c> answers "which group does this element belong to" (keyed by
    /// element identifier). Both are built in one pass so they cannot diverge.
    /// </summary>
    private sealed record ElementGroups(
        ILookup<string, Element> ByGroup, Dictionary<long, string> GroupByElementId);

    private IList<Element> GetCachedTargetElements(Document document, View? view)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(document, CollisionQueryNames.TargetElements, view, argument: TakeFormula),
            () => GetElements(document, view));

    private IList<Element> GetElements(Document document, View? view)
    {
        FilteredElementCollector collector = view is null
            ? new FilteredElementCollector(document)
            : new FilteredElementCollector(document, view.Id);
        return collector
            .WherePasses(new ElementIsElementTypeFilter(true))
            .WherePasses(GetModelCategoryFilter(document))
            .WherePasses(Filter)
            .ToElements();
    }

    private static LogicalOrFilter GetModelCategoryFilter(Document document)
    {
        List<ElementFilter> categoryFilters = document.Settings.Categories
            .Cast<Category>().Where(i => i.CategoryType == CategoryType.Model)
            .Select(i => new ElementCategoryFilter(i.Id.ToBuiltInCategory()))
            .ToList<ElementFilter>();
        return new LogicalOrFilter(categoryFilters);
    }

    // Inputs are already non-degenerate: ElementGeometryExtensions.CollectSolids keeps only solids
    // with Volume greater than Epsilon, so no zero-volume guard is needed here. The result volume
    // is the intersection test itself - ExecuteBooleanOperation returns a solid, not a boolean -
    // and Epsilon additionally filters out touches and slivers.
    private bool HasIntersection(
        IEnumerable<Solid> solids1,
        IEnumerable<Solid> solids2,
        long elementId,
        long targetElementId)
    {
        foreach (Solid solid in solids1)
        {
            foreach (Solid targetSolid in solids2)
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