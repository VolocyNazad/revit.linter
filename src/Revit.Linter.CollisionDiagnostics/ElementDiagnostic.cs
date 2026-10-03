using Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;
using Revit.Linter.CollisionDiagnostics.Infrastructure;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Spatial;
using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Sugar;
using Microsoft.Extensions.Logging;

namespace Revit.Linter.CollisionDiagnostics;

internal sealed class ElementDiagnostic(
    ElementFilterFactory elementFilterFactory,
    ElementFunctionFactory elementFunctionFactory,
    IGetElementBoundingBoxService getElementBoundingBox,
    IGetElementGeometryService getElementGeometry,
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
    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        IReadOnlyCollection<Solid> targetSolids = getElementGeometry.Execute(targetElement, view);

        if (targetSolids.Count == 0) return DiagnosticFeedback.Valid;

        BoundingBoxXYZ targetBoundingBox = getElementBoundingBox.Execute(targetElement, view);

        // The group of an element was already computed when the groups were built, so the formula is
        // evaluated again only for a target outside the collected elements.
        long targetElementId = targetElement.Id.Value();
        ElementGroups groups = GetGroups(document, view);
        string group = groups.GroupByElementId.TryGetValue(targetElementId, out string? knownGroup)
            ? knownGroup
            : GetGroup(targetElement);

        // Built once per (document, view, formulas, group) and reused by every target element's Execute
        // call for that group - see BoundingBoxGridIndex for why this replaces a linear scan of the group
        // (O(N) bounding-box comparisons per target -> O(N^2) total for the group).
        BoundingBoxGridIndex spatialIndex = documentQueries.GetOrCreate(
            DocumentQueryKey.Create(
                document, CollisionQueryNames.SpatialIndex, view,
                argument: $"{TakeFormula}{ArgumentSeparator}{GroupByFormula}{ArgumentSeparator}{group}"),
            () => BoundingBoxGridIndex.Build(
                groups.ByGroup[group],
                element => getElementBoundingBox.Execute(element, view)));

        // Every intersecting element is collected, not only the first one found: the finding then shows
        // the complete set of collisions of the target and does not depend on the order of candidates.
        List<Element> intersections = [];

        foreach (Element element in spatialIndex.Query(targetBoundingBox))
        {
            long elementId = element.Id.Value();

            if (elementId == targetElementId) continue;

            BoundingBoxXYZ boundingBox = getElementBoundingBox.Execute(element, view);

            if (!boundingBox.Overlaps(targetBoundingBox)) continue;

            IReadOnlyCollection<Solid> solids = getElementGeometry.Execute(element, view);

            if (HasIntersectionCached(document, view, elementId, targetElementId, solids, targetSolids))
                intersections.Add(element);
        }

        if (intersections.Count == 0) return DiagnosticFeedback.Valid;

        // Ordered by identifier so the message and the dependency list are the same on every run.
        intersections.Sort((first, second) => first.Id.Value().CompareTo(second.Id.Value()));

        return new(DiagnosticVerdict.NotValid,
            new() {
                { "intersection.elementNames", string.Join(", ", intersections.Select(element => element.Name)) },
                { "intersection.elementIds", intersections.Select(element => element.Id).ToArray() },
                { "intersection.count", intersections.Count },
            },
            intersections.ToArray<object>()
        );
    }

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
        long elementId,
        long targetElementId,
        IReadOnlyCollection<Solid> solids,
        IReadOnlyCollection<Solid> targetSolids)
    {
        (long minId, long maxId) = elementId < targetElementId
            ? (elementId, targetElementId)
            : (targetElementId, elementId);

        return documentQueries.GetOrCreate(
            DocumentQueryKey.Create(
                document, CollisionQueryNames.PairIntersects, view, argument: $"{minId}:{maxId}"),
            () => HasIntersection(solids, targetSolids, elementId, targetElementId));
    }

    private ElementFilter Filter => field ??= elementFilterFactory.Create(TakeFormula);
    private Func<Element, object> GroupByDelegate => field ??= elementFunctionFactory.Create(GroupByFormula);

    private string GetGroup(Element element) => GroupByDelegate.Invoke(element)?.ToString() ?? string.Empty;

    // The keys carry the formulas rather than the rule code: rules with the same formulas share the
    // collected elements and groups, and rules with different formulas can never read each other's.
    private ElementGroups GetGroups(Document document, View? view)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(
                document, CollisionQueryNames.Groups, view,
                argument: $"{TakeFormula}{ArgumentSeparator}{GroupByFormula}"),
            () => BuildGroups(GetTargetElements(document, view)));

    private ElementGroups BuildGroups(IList<Element> elements)
    {
        Dictionary<long, string> groupByElementId = new(elements.Count);
        ILookup<string, Element> byGroup = elements.ToLookup(element =>
        {
            string group = GetGroup(element);
            groupByElementId[element.Id.Value()] = group;
            return group;
        });
        return new ElementGroups(byGroup, groupByElementId);
    }

    private sealed record ElementGroups(
        ILookup<string, Element> ByGroup, Dictionary<long, string> GroupByElementId);

    private IList<Element> GetTargetElements(Document document, View? view)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(document, CollisionQueryNames.TargetElements, view, argument: TakeFormula),
            () => GetElements(document, view));

    private IList<Element> GetElements(Document document, View? view)
    {
        List<ElementFilter> categoryFilters = document.Settings.Categories
            .Cast<Category>().Where(i => i.CategoryType == CategoryType.Model)
            .Select(i => new ElementCategoryFilter(i.Id.ToBuiltInCategory()))
            .Cast<ElementFilter>().ToList();
        if (view is null)
            return new FilteredElementCollector(document)
                .WherePasses(new ElementIsElementTypeFilter(true))
                .WherePasses(new LogicalOrFilter(categoryFilters))
                .WherePasses(Filter)
                .ToElements();
        return new FilteredElementCollector(document, view.Id)
            .WherePasses(new ElementIsElementTypeFilter(true))
            .WherePasses(new LogicalOrFilter(categoryFilters))
            .WherePasses(Filter)
            .ToElements();
    }
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