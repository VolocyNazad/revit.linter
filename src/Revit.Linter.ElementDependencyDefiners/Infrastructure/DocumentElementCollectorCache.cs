using Autodesk.Revit.DB;
using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.ElementDependencyDefiners.Infrastructure;

/// <summary>
/// Caches elements returned by document-wide collectors.
/// </summary>
/// <remarks>
/// Dependency definers are static extension methods and formula-created objects, so they reach the
/// cached document queries through this static entry point instead of constructor injection.
/// </remarks>
public static class DocumentElementCollectorCache
{
    private const string CollectorQuery = "element-dependency-definers:collector";

    private static IDocumentQueryService? _documentQueries;

    /// <summary>
    /// Configures the cached document queries used by element collectors.
    /// </summary>
    /// <param name="documentQueries">The query service that owns the transaction-bound cache.</param>
    public static void Initialize(IDocumentQueryService documentQueries)
        => _documentQueries = documentQueries ?? throw new ArgumentNullException(nameof(documentQueries));

    internal static IDocumentQueryService Queries
        => _documentQueries
           ?? throw new InvalidOperationException(
               $"{nameof(DocumentElementCollectorCache)} is not initialized.");

    internal static IReadOnlyList<Element> GetOrCreate(
        Document document,
        string collectorKey,
        Func<IEnumerable<Element>> factory)
        => Queries.GetOrCreate(
            DocumentQueryKey.Create(document, CollectorQuery, argument: collectorKey),
            () => factory().ToArray());
}