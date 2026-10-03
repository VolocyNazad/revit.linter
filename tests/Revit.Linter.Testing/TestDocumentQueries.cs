using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.DocumentQueries.Infrastructure.Services;
using Revit.TransactionMemoryCache.Abstractions.Services;

namespace Revit.Linter.Testing;

/// <summary>
/// Creates the production <see cref="IDocumentQueryService"/> over a test cache.
/// </summary>
public static class TestDocumentQueries
{
    /// <summary>
    /// Creates a query service whose results live as long as <paramref name="cache"/>.
    /// </summary>
    /// <param name="cache">
    /// The cache to use; a new <see cref="TestTransactionMemoryCache"/> when omitted. That cache never
    /// invalidates, so create a new service after changing the document.
    /// </param>
    public static IDocumentQueryService Create(IRevitTransactionMemoryCache? cache = null)
        => new DocumentQueryService(cache ?? new TestTransactionMemoryCache());
}