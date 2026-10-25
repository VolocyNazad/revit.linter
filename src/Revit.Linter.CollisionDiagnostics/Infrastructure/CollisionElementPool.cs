using Revit.Linter.CollisionDiagnostics.Infrastructure.Extensions;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.Languages.Factories;
using Revit.Sugar;

namespace Revit.Linter.CollisionDiagnostics.Infrastructure;

/// <summary>Collects the elements a collision rule selects and resolves their group.</summary>
/// <remarks>
/// The <c>take</c> filter and the <c>groupBy</c> formula belong to one rule, so one pool serves one
/// rule. The collected elements are cached under the take formula, and model categories do not change
/// during a run, so the category filter is built once per document. A host view never applies inside
/// a linked document, so linked elements are collected document-wide with default geometry options.
/// </remarks>
internal sealed class CollisionElementPool(
    ElementFilterFactory elementFilterFactory,
    ElementFunctionFactory elementFunctionFactory,
    IDocumentQueryService documentQueries,
    string takeFormula,
    string groupByFormula)
{
    // Stateless and immutable, so one instance serves every collector.
    private static readonly ElementIsElementTypeFilter NotElementTypeFilter = new(true);

    private ElementFilter? _filter;
    private Func<Element, object>? _groupBy;

    /// <summary>Gets the take formula this pool was built for.</summary>
    internal string TakeFormula => takeFormula;

    /// <summary>Gets the grouping formula this pool was built for.</summary>
    internal string GroupByFormula => groupByFormula;

    /// <summary>Gets the elements selected by the take formula, or the ones visible in <paramref name="view"/>.</summary>
    /// <param name="document">The queried document.</param>
    /// <param name="view">The view to limit the selection to, or <see langword="null"/> for the whole document.</param>
    /// <returns>The cached element list.</returns>
    internal IList<Element> Collect(Document document, View? view) =>
        documentQueries.GetCached(
            document, view, CollisionQueryNames.TargetElements,
            () => CollectUncached(document, view),
            takeFormula);

    /// <summary>Gets the group of one element according to the grouping formula.</summary>
    /// <param name="element">The grouped element.</param>
    /// <returns>The group name; an empty string when the formula returns no value.</returns>
    internal string GroupOf(Element element) =>
        GroupBy.Invoke(element)?.ToString() ?? string.Empty;

    private IList<Element> CollectUncached(Document document, View? view)
    {
        FilteredElementCollector collector = view is null
            ? new FilteredElementCollector(document)
            : new FilteredElementCollector(document, view.Id);
        return collector
            .WherePasses(NotElementTypeFilter)
            .WherePasses(GetModelCategoryFilter(document))
            .WherePasses(Filter)
            .ToElements();
    }

    private ElementFilter Filter => _filter ??= elementFilterFactory.Create(takeFormula);

    private Func<Element, object> GroupBy =>
        _groupBy ??= elementFunctionFactory.Create<object>(groupByFormula, string.Empty);

    // Model categories do not change during a run: the filter is built once per document
    // instead of on every target-elements cache miss (once per unique take formula).
    private LogicalOrFilter GetModelCategoryFilter(Document document) =>
        documentQueries.GetCached(
            document, null, CollisionQueryNames.ModelCategoryFilter,
            () => BuildModelCategoryFilter(document));

    private static LogicalOrFilter BuildModelCategoryFilter(Document document)
    {
        List<ElementFilter> categoryFilters = document.Settings.Categories
            .Cast<Category>().Where(i => i.CategoryType == CategoryType.Model)
            .Select(i => new ElementCategoryFilter(i.Id.ToBuiltInCategory()))
            .ToList<ElementFilter>();
        return new LogicalOrFilter(categoryFilters);
    }
}
