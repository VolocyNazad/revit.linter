using Revit.Linter.CollisionDiagnostics.Abstractions.Infrasructure.Services;
using Revit.Linter.CollisionDiagnostics.Infrasructure.Extensions;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.TransactionMemoryCache.Abstractions.Services;

namespace Revit.Linter.CollisionDiagnostics;

internal sealed class ElementDiagnostic(
    ElementFilterFactory elementFilterFactory, 
    ElementFunctionFactory elementFunctionFactory,
    IGetElementBoundingBoxService getElementBoundingBox,
    IGetElementGeomentryService getElementGeomentry,
    IRevitTransactionMemoryCache revitTransactionMemoryCache) : IElementDiagnostic
{
    private const double EPSILON = 1e-6;

    public required ElementDiagnosticId Identity { get; init; }
    public required string TakeFormula { get; init; }
    public required string GroupByFormula { get; init; }
    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
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

        var targetSolids = getElementGeomentry.Execute(targetElementId, targetGeometryElement);

        if (targetSolids.Count == 0) return DiagnosticResult.Valid;

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

        foreach (Element element in elements) 
        {
            var elementId = element.Id.Value();

            if (elementId == targetElementId) continue;

            GeometryElement? geometryElement = revitTransactionMemoryCache
                .GetOrCreate($"element:element-geometry:id:{elementId}", () => element.get_Geometry(options)) ?? throw new InvalidOperationException($"Failed to get object from cache.");

            var boundingBox = getElementBoundingBox.Execute(elementId, geometryElement);

            if (!HasIntersection(boundingBox, targetBoundingBox)) continue;

            var solids = getElementGeomentry.Execute(elementId, geometryElement);

            if (HasIntersection(solids, targetSolids))
                return new(DiagnosticVerdict.NotValid, new() {
                    { "intersection.elementName", element.Name },
                    { "intersection.elementId", element.Id },
                });
        }

        return DiagnosticResult.Valid;
    }

    private ElementFilter Filter => field ??= elementFilterFactory.Create(TakeFormula);
    private Func<Element, object> GroupByDelegate => field ??= elementFunctionFactory.Create(GroupByFormula);
    private IList<Element> GetElements(Document document, View? view)
    {
        List<ElementFilter> categoryFilters = document.Settings.Categories
            .Cast<Category>().Where(i => i.CategoryType == CategoryType.Model)
            .Select(i => new ElementCategoryFilter((BuiltInCategory)i.Id.Value()))
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
    private static bool HasIntersection(BoundingBoxXYZ box1, BoundingBoxXYZ box2)
    {
        // Теорема разделяющей оси
        return !(box1.Max.X < box2.Min.X || box2.Max.X < box1.Min.X ||
                 box1.Max.Y < box2.Min.Y || box2.Max.Y < box1.Min.Y ||
                 box1.Max.Z < box2.Min.Z || box2.Max.Z < box1.Min.Z);
    }
    private static bool HasIntersection(IEnumerable<Solid> solids1, IEnumerable<Solid> solids2)
    {
        foreach (Solid solid in solids1)
        {
            foreach (Solid targetSolid in solids2)
            {
                if (BooleanOperationsUtils.ExecuteBooleanOperation(
                    targetSolid, solid, BooleanOperationsType.Intersect).Volume > EPSILON) return true;
            }
        }
        return false;
    }
}
