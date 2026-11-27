using System.ComponentModel;
using Revit.Linter.Presentation.Abstractions.Services;
using Revit.Linter.Presentation.Models;
using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.Presentation.Services;

/// <summary>
/// Keeps the saved table view and persists it to <c>tables.yml</c>.
/// </summary>
/// <remarks>
/// Only a non-default view is stored: a table without an entry keeps its default search, filters and sort.
/// The service reads the current settings on every call, so a file changed externally is used by the next
/// table that asks for its state.
/// </remarks>
public sealed class TableStateService : ITableStateService
{
    private readonly IValueStore<TableStateSettings> _store;

    /// <summary>Initializes the service from the saved table settings.</summary>
    /// <param name="store">Persists the table view state.</param>
    public TableStateService(IValueStore<TableStateSettings> store)
        => _store = store ?? throw new ArgumentNullException(nameof(store));

    /// <inheritdoc />
    public TableSortPreference? GetSort(string tableId) =>
        _store.CurrentValue.Sorts.FirstOrDefault(
            sort => string.Equals(sort.Table, tableId, StringComparison.Ordinal));

    /// <inheritdoc />
    public void SetSort(string tableId, string columnKey, ListSortDirection direction) =>
        _store.Update(settings =>
        {
            settings.Sorts.RemoveAll(sort => string.Equals(sort.Table, tableId, StringComparison.Ordinal));
            settings.Sorts.Add(new TableSortPreference
            {
                Table = tableId,
                Column = columnKey,
                Descending = direction == ListSortDirection.Descending,
            });
        });

    /// <inheritdoc />
    public TableSearchPreference? GetSearch(string tableId) =>
        _store.CurrentValue.Searches.FirstOrDefault(
            search => string.Equals(search.Table, tableId, StringComparison.Ordinal));

    /// <inheritdoc />
    public void SetSearch(string tableId, string text, bool matchCase, bool matchWholeWord) =>
        _store.Update(settings =>
        {
            settings.Searches.RemoveAll(search => string.Equals(search.Table, tableId, StringComparison.Ordinal));
            if (string.IsNullOrEmpty(text) && !matchCase && !matchWholeWord) return;

            settings.Searches.Add(new TableSearchPreference
            {
                Table = tableId,
                Text = text,
                MatchCase = matchCase,
                MatchWholeWord = matchWholeWord,
            });
        });

    /// <inheritdoc />
    public bool IsFilterActive(string tableId, string filterGroup, string filterKey) =>
        !_store.CurrentValue.InactiveFilters.Contains(
            GetFilterKey(tableId, filterGroup, filterKey), StringComparer.Ordinal);

    /// <inheritdoc />
    public void SetFilterActive(string tableId, string filterGroup, string filterKey, bool isActive) =>
        _store.Update(settings =>
        {
            string key = GetFilterKey(tableId, filterGroup, filterKey);
            if (isActive)
            {
                settings.InactiveFilters.RemoveAll(stored => string.Equals(stored, key, StringComparison.Ordinal));
                return;
            }

            if (!settings.InactiveFilters.Contains(key, StringComparer.Ordinal))
                settings.InactiveFilters.Add(key);
        });

    private static string GetFilterKey(string tableId, string filterGroup, string filterKey) =>
        $"{tableId}/{filterGroup}/{filterKey}";
}
