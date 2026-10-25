namespace Revit.Linter.DocumentQueries.Infrastructure;

/// <summary>Names the document queries the shared query service caches.</summary>
/// <remarks>
/// The transaction cache key carries the name as a string, while the closed set of queries the service
/// issues is an enumeration: a caller cannot pass an arbitrary or misspelled name, and the set is
/// declared once next to the query service.
/// </remarks>
internal enum DocumentQuery
{
    Elements,
    ElementsOfClass,
    ElementTypes,
    Geometry,
}

internal static class DocumentQueryExtensions
{
    private const string Prefix = "document:";

    /// <summary>Gets the cache name of the query, prefixed with the owning module.</summary>
    /// <param name="query">The document query.</param>
    /// <returns>The query name stored in the document query key.</returns>
    internal static string CacheName(this DocumentQuery query) => Prefix + query;
}
