using System.Diagnostics;
using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Sugar;
using Revit.TransactionMemoryCache.Abstractions.Services;

namespace Revit.Linter.DocumentQueries.Infrastructure.Services;

internal sealed class DocumentQueryService(IRevitTransactionMemoryCache transactionMemoryCache)
    : IDocumentQueryService
{
    private readonly Dictionary<string, QueryCounter> _counters = new(StringComparer.Ordinal);
    private ElementFilter? _allElementsFilter;

    public IReadOnlyList<Element> GetElements(Document document, View? view = null)
        => GetOrCreate(
            DocumentQueryKey.Create(document, DocumentQueryNames.Elements, view),
            () => CreateCollector(document, view)
                .WherePasses(_allElementsFilter ??= ElementFilterUtils.AllFilter())
                .ToElements()
                .ToArray());

    public IReadOnlyList<TElement> GetElementsOfClass<TElement>(Document document)
        where TElement : Element
        => GetOrCreate(
            DocumentQueryKey.Create(
                document, DocumentQueryNames.ElementsOfClass, argument: typeof(TElement).FullName),
            () => new FilteredElementCollector(document)
                .OfClass(typeof(TElement))
                .Cast<TElement>()
                .ToArray());

    public IReadOnlyList<Element> GetElementTypes(Document document)
        => GetOrCreate(
            DocumentQueryKey.Create(document, DocumentQueryNames.ElementTypes),
            () => new FilteredElementCollector(document)
                .WhereElementIsElementType()
                .ToElements()
                .ToArray());

    // An element without geometry yields null, so this query bypasses the non-null guard of GetOrCreate.
    public GeometryElement? GetGeometry(Element element, View? view = null)
        => GetOrCreateCounted(
            DocumentQueryKey.Create(element.Document, DocumentQueryNames.Geometry, view, element.Id),
            () => element.get_Geometry(view is null ? new Options() : new Options { View = view }));

    public TItem GetOrCreate<TItem>(DocumentQueryKey key, Func<TItem> factory)
    {
        TItem? item = GetOrCreateCounted(key, factory);
        if (item is null)
            throw new InvalidOperationException(
                $"The transaction cache produced no value for the '{key.Query}' query.");

        return item;
    }

    public IReadOnlyList<DocumentQueryStatistics> TakeStatistics()
    {
        DocumentQueryStatistics[] statistics = _counters
            .Select(pair => new DocumentQueryStatistics(
                pair.Key,
                pair.Value.Hits,
                pair.Value.Misses,
                pair.Value.MissTicks * 1000.0 / Stopwatch.Frequency))
            .OrderByDescending(item => item.MissMilliseconds)
            .ToArray();
        _counters.Clear();
        return statistics;
    }

    // The factory runs only on a cache miss, so whether it ran tells a hit from a miss.
    private TItem? GetOrCreateCounted<TItem>(DocumentQueryKey key, Func<TItem> factory)
    {
        bool missed = false;
        long missTicks = 0;
        TItem? item = transactionMemoryCache.GetOrCreate<TItem>(key, () =>
        {
            missed = true;
            long started = Stopwatch.GetTimestamp();
            try
            {
                return factory();
            }
            finally
            {
                missTicks = Stopwatch.GetTimestamp() - started;
            }
        });

        if (!_counters.TryGetValue(key.Query, out QueryCounter? counter))
        {
            counter = new QueryCounter();
            _counters[key.Query] = counter;
        }

        if (missed)
        {
            counter.Misses++;
            counter.MissTicks += missTicks;
        }
        else
        {
            counter.Hits++;
        }

        return item;
    }

    private static FilteredElementCollector CreateCollector(Document document, View? view)
        => view is null
            ? new FilteredElementCollector(document)
            : new FilteredElementCollector(document, view.Id);

    private sealed class QueryCounter
    {
        public int Hits { get; set; }
        public int Misses { get; set; }
        public long MissTicks { get; set; }
    }
}