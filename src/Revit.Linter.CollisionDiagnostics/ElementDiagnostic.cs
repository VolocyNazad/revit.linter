using Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Spatial;
using Toolkit.Revit.Extensions;
using Revit.TransactionMemoryCache.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Revit.Linter.CollisionDiagnostics;

internal sealed class ElementDiagnostic(
    ElementFilterFactory elementFilterFactory, 
    ElementFunctionFactory elementFunctionFactory,
    IGetElementBoundingBoxService getElementBoundingBox,
    IGetElementGeometryService getElementGeometry,
    IRevitTransactionMemoryCache revitTransactionMemoryCache,
    ILogger<ElementDiagnostic> logger) : IElementDiagnostic
{
    private const double Epsilon = 1e-6;

    public required ElementDiagnosticId Identity { get; init; }
    public required string TakeFormula { get; init; }
    public required string GroupByFormula { get; init; }
    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement) //todo в результат попадает 2 пересечки (1 с 2,  2 с 1)
    {
        var targetElementId = targetElement.Id.Value();

        Options? options = revitTransactionMemoryCache
            .GetOrCreate("options", () => view is null 
                ? new Options() 
                : new Options() { View = view }
            );

        GeometryElement? targetGeometryElement = revitTransactionMemoryCache
            .GetOrCreate($"element:element-geometry:id:{targetElementId}", () 
                => targetElement.get_Geometry(options)) ?? throw new InvalidOperationException($"Failed to get object from cache.");

        var targetSolids = getElementGeometry.Execute(targetElementId, targetGeometryElement);

        if (targetSolids.Count == 0) return DiagnosticFeedback.Valid;

        var targetBoundingBox = getElementBoundingBox.Execute(targetElementId, targetGeometryElement);

        var key = GroupByDelegate.Invoke(targetElement)?.ToString() ?? string.Empty;
        IList<Element> elements = revitTransactionMemoryCache
            .GetOrCreate($"target-elements:document:{document.Title}:{key}", () =>
            {
                IList<Element> elements = revitTransactionMemoryCache
                .GetOrCreate($"target-elements:document:{document.Title}", ()
                    => GetElements(document, view)) ?? throw new InvalidOperationException($"Failed to get object from cache.");

                return elements.Where(element => key.Equals(GroupByDelegate.Invoke(element)?.ToString() ?? string.Empty)).ToList();
            }) ?? throw new InvalidOperationException($"Failed to get object from cache.");

        // Built once per (document, rule group) and reused by every target element's Execute call
        // for that group - see BoundingBoxGridIndex for why this replaces the former linear scan
        // of `elements` (O(N) bounding-box comparisons per target -> O(N^2) total for the group).
        BoundingBoxGridIndex spatialIndex = revitTransactionMemoryCache
            .GetOrCreate($"target-elements:spatial-index:document:{document.Title}:{key}", ()
                => BoundingBoxGridIndex.Build(elements, element => getElementBoundingBox.Execute(element, view)))
            ?? throw new InvalidOperationException($"Failed to get object from cache.");

        foreach (Element element in spatialIndex.Query(targetBoundingBox))
        {
            var elementId = element.Id.Value();

            if (elementId == targetElementId) continue;

            GeometryElement? geometryElement = revitTransactionMemoryCache
                .GetOrCreate($"element:element-geometry:id:{elementId}", () => element.get_Geometry(options))
                ?? throw new InvalidOperationException($"Failed to get object from cache.");

            var boundingBox = getElementBoundingBox.Execute(elementId, geometryElement);

            if (!boundingBox.Overlaps(targetBoundingBox)) continue;

            var solids = getElementGeometry.Execute(elementId, geometryElement);

            if (HasIntersectionCached(elementId, targetElementId, solids, targetSolids))
                return new(DiagnosticVerdict.NotValid,
                    new() {
                        { "intersection.elementName", element.Name },
                        { "intersection.elementId", element.Id },
                    },
                    element
                );
        }

        return DiagnosticFeedback.Valid;
    }

    // The unordered pair (elementId, targetElementId) is evaluated from both directions across
    // the outer per-element loop in DiagnosticService: once with this element as the target, once
    // with the other element as the target. Without caching, the expensive Boolean solid
    // intersection would be executed twice for every colliding (or bounding-box-overlapping) pair.
    // Caching the result per rule + unordered pair keeps both directions' reports intact (each
    // element still gets its own diagnostic feedback) while computing the intersection only once.
    //
    // elementId/targetElementId are ElementId.Value() (int on BEFORE2024 targets, long on 2025+),
    // matching IGetElementGeometryService/IGetElementBoundingBoxService, which likewise branch
    // between int and long overloads per target framework.
#if BEFORE2024
    private bool HasIntersectionCached(
        int elementId,
        int targetElementId,
        IReadOnlyCollection<Solid> solids,
        IReadOnlyCollection<Solid> targetSolids)
    {
        (int minId, int maxId) = elementId < targetElementId
            ? (elementId, targetElementId)
            : (targetElementId, elementId);
#else
    private bool HasIntersectionCached(
        long elementId,
        long targetElementId,
        IReadOnlyCollection<Solid> solids,
        IReadOnlyCollection<Solid> targetSolids)
    {
        (long minId, long maxId) = elementId < targetElementId
            ? (elementId, targetElementId)
            : (targetElementId, elementId);
#endif

        IntersectionResult result = revitTransactionMemoryCache
            .GetOrCreate($"collision:{Identity.Code}:pair-intersects:{minId}:{maxId}", ()
                => new IntersectionResult(HasIntersection(solids, targetSolids, elementId, targetElementId)))
            ?? throw new InvalidOperationException($"Failed to get object from cache.");

        return result.Value;
    }

    private sealed record IntersectionResult(bool Value);

    private ElementFilter Filter => field ??= elementFilterFactory.Create(TakeFormula);
    private Func<Element, object> GroupByDelegate => field ??= elementFunctionFactory.Create(GroupByFormula);
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
