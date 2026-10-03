using Revit.Linter.DocumentQueries.Abstractions.Models;

namespace Revit.Linter.DocumentQueries.Abstractions.Services;

/// <summary>
/// Answers document queries shared by diagnostics and keeps the answers in the transaction cache.
/// </summary>
/// <remarks>
/// Every result is cached under a <see cref="DocumentQueryKey"/> until the transaction cache is
/// invalidated, so callers must not modify the returned collections. All members require a valid
/// Revit API context.
/// </remarks>
public interface IDocumentQueryService
{
    /// <summary>
    /// Gets every element of the document, or every element visible in <paramref name="view"/>.
    /// </summary>
    /// <param name="document">The queried document.</param>
    /// <param name="view">The view to limit the query to, or <see langword="null"/> for the whole document.</param>
    /// <returns>The cached element list.</returns>
    IReadOnlyList<Element> GetElements(Document document, View? view = null);

    /// <summary>
    /// Gets every element of the document that is an instance of <typeparamref name="TElement"/>.
    /// </summary>
    /// <typeparam name="TElement">A class supported by the Revit class filter.</typeparam>
    /// <param name="document">The queried document.</param>
    /// <returns>The cached element list.</returns>
    IReadOnlyList<TElement> GetElementsOfClass<TElement>(Document document)
        where TElement : Element;

    /// <summary>
    /// Gets every element type of the document.
    /// </summary>
    /// <param name="document">The queried document.</param>
    /// <returns>The cached element type list.</returns>
    IReadOnlyList<Element> GetElementTypes(Document document);

    /// <summary>
    /// Gets the geometry of <paramref name="element"/>.
    /// </summary>
    /// <param name="element">The element whose geometry is requested.</param>
    /// <param name="view">The view the geometry is computed for, or <see langword="null"/> for default options.</param>
    /// <returns>The cached geometry, or <see langword="null"/> when the element has none.</returns>
    /// <remarks>Geometry computed for a view is cached separately from the default geometry.</remarks>
    GeometryElement? GetGeometry(Element element, View? view = null);

    /// <summary>
    /// Gets the value cached under <paramref name="key"/>, creating it with <paramref name="factory"/> on a miss.
    /// </summary>
    /// <typeparam name="TItem">The cached value type. One key must always be used with one type.</typeparam>
    /// <param name="key">The query key.</param>
    /// <param name="factory">Creates the value when the cache has none.</param>
    /// <returns>The cached or newly created value.</returns>
    /// <exception cref="InvalidOperationException">The cache produced no value.</exception>
    TItem GetOrCreate<TItem>(DocumentQueryKey key, Func<TItem> factory);

    /// <summary>
    /// Returns the cache statistics gathered since the previous call and starts a new measurement.
    /// </summary>
    /// <returns>One entry per query name that was requested, slowest misses first.</returns>
    /// <remarks>
    /// Many misses for a query that a previous run already answered mean the transaction cache was
    /// invalidated in between. The counters are not synchronized and belong to the Revit API thread.
    /// </remarks>
    IReadOnlyList<DocumentQueryStatistics> TakeStatistics();
}