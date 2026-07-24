using Autodesk.Revit.DB;
using Revit.TransactionMemoryCache.Abstractions.Services;

namespace Revit.Linter.ElementDependencyDefiners.Infrastructure;

/// <summary>
/// Caches element identifiers returned by document-wide collectors.
/// </summary>
public static class DocumentElementCollectorCache
{
    private static IRevitTransactionMemoryCache? _cache;

    /// <summary>
    /// Configures the transaction-bound cache used by element collectors.
    /// </summary>
    public static void Initialize(IRevitTransactionMemoryCache cache)
        => _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    internal static IEnumerable<Element> GetOrCreate(
        Document document,
        string collectorKey,
        Func<IEnumerable<ElementId>> factory)
    {
        IRevitTransactionMemoryCache cache = _cache
            ?? throw new InvalidOperationException(
                $"{nameof(DocumentElementCollectorCache)} is not initialized.");
        string key = $"element-dependency-definers:collector:document:{document.Title}:query:{collectorKey}";
        ElementId[] elementIds = cache.GetOrCreate(key, () => factory().ToArray()) ?? [];

        foreach (ElementId elementId in elementIds)
            if (document.GetElement(elementId) is { } element)
                yield return element;
    }

}
