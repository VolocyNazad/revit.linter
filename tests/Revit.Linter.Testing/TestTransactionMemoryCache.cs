using Revit.TransactionMemoryCache.Abstractions.Services;

namespace Revit.Linter.Testing;

/// <summary>
/// An in-memory <see cref="IRevitTransactionMemoryCache"/> for tests: it keeps every created item for the
/// lifetime of the instance and never invalidates on Revit transactions.
/// </summary>
public sealed class TestTransactionMemoryCache : IRevitTransactionMemoryCache
{
    private readonly Dictionary<object, object?> _items = [];

    /// <inheritdoc />
    public TItem? GetOrCreate<TItem>(object key, Func<TItem> factory)
    {
        if (_items.TryGetValue(key, out object? value))
            return (TItem?)value;

        TItem item = factory();
        _items[key] = item;
        return item;
    }
}
