using Revit.Linter.Presentation.Abstractions.Models;
using Revit.Linter.Presentation.Abstractions.Services;
using Revit.Linter.Presentation.Models;
using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.Presentation.Services;

/// <summary>
/// Keeps the hidden table columns and the column widths in memory and persists them to <c>columns.yml</c>.
/// </summary>
/// <remarks>
/// A column is visible unless its <c>table/column</c> key is present, so a new column needs no migration.
/// A column keeps the width its table declares unless the user dragged its header. The reveal scope of a
/// guided action changes only the in-memory visibility, never the stored preference.
/// </remarks>
public sealed class TableColumnsService : ITableColumnsService, IDisposable
{
    private const double WidthTolerance = 0.5;

    private readonly IValueStore<TableColumnsSettings> _store;
    private readonly HashSet<string> _hidden;
    private readonly Dictionary<(string Table, string Column), double> _widths;
    private readonly IDisposable _subscription;
    private int _disposed;

    /// <summary>Initializes the service from the saved settings.</summary>
    /// <param name="store">Persists the hidden columns and the column widths.</param>
    public TableColumnsService(IValueStore<TableColumnsSettings> store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _hidden = new HashSet<string>(store.CurrentValue.Hidden ?? [], StringComparer.Ordinal);
        _widths = ToWidths(store.CurrentValue.Widths);
        _subscription = store.OnChange(ApplyStoredSettings);
    }

    /// <inheritdoc />
    public event EventHandler<TableColumnsChangedEventArgs>? Changed;

    /// <inheritdoc />
    public bool IsVisible(string tableId, string columnKey)
        => !_hidden.Contains(GetKey(tableId, columnKey));

    /// <inheritdoc />
    public double? GetWidth(string tableId, string columnKey) =>
        _widths.TryGetValue((tableId, columnKey), out double pixels) ? pixels : null;

    /// <inheritdoc />
    public void SetWidth(string tableId, string columnKey, double pixels)
    {
        if (!IsUsableWidth(pixels)) return;

        (string, string) key = (tableId, columnKey);
        if (_widths.TryGetValue(key, out double current) && Math.Abs(current - pixels) < WidthTolerance) return;

        _widths[key] = pixels;
        Persist();
    }

    /// <inheritdoc />
    public void SetVisibility(string tableId, string columnKey, bool isVisible)
    {
        bool changed = isVisible
            ? _hidden.Remove(GetKey(tableId, columnKey))
            : _hidden.Add(GetKey(tableId, columnKey));
        if (!changed) return;

        Persist();
        Changed?.Invoke(this, new TableColumnsChangedEventArgs(tableId));
    }

    /// <inheritdoc />
    public void ShowAll(string tableId)
    {
        string prefix = tableId + "/";
        if (_hidden.RemoveWhere(key => key.StartsWith(prefix, StringComparison.Ordinal)) == 0) return;

        Persist();
        Changed?.Invoke(this, new TableColumnsChangedEventArgs(tableId));
    }

    /// <inheritdoc />
    public IDisposable Reveal(string tableId, string columnKey)
    {
        string key = GetKey(tableId, columnKey);
        bool restoreHidden = _hidden.Remove(key);
        if (restoreHidden)
            Changed?.Invoke(this, new TableColumnsChangedEventArgs(tableId));

        return new RevealScope(this, tableId, key, restoreHidden);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _subscription.Dispose();
    }

    private void ApplyStoredSettings(TableColumnsSettings settings)
    {
        var storedHidden = new HashSet<string>(settings.Hidden ?? [], StringComparer.Ordinal);
        Dictionary<(string Table, string Column), double> storedWidths = ToWidths(settings.Widths);
        bool hiddenChanged = !storedHidden.SetEquals(_hidden);
        bool widthsChanged = !WidthsEqual(storedWidths);
        if (!hiddenChanged && !widthsChanged) return;

        _hidden.Clear();
        _hidden.UnionWith(storedHidden);
        _widths.Clear();
        foreach (KeyValuePair<(string Table, string Column), double> width in storedWidths)
            _widths[width.Key] = width.Value;

        Changed?.Invoke(this, new TableColumnsChangedEventArgs(tableId: null));
    }

    private void Restore(string tableId, string key)
    {
        if (!_hidden.Add(key)) return;
        Changed?.Invoke(this, new TableColumnsChangedEventArgs(tableId));
    }

    private void Persist() =>
        _store.Update(settings =>
        {
            settings.Hidden = _hidden.OrderBy(key => key, StringComparer.Ordinal).ToList();
            settings.Widths = _widths
                .OrderBy(width => width.Key.Table, StringComparer.Ordinal)
                .ThenBy(width => width.Key.Column, StringComparer.Ordinal)
                .Select(width => new TableColumnWidth
                {
                    Table = width.Key.Table,
                    Column = width.Key.Column,
                    Pixels = width.Value,
                })
                .ToList();
        });

    private static Dictionary<(string Table, string Column), double> ToWidths(List<TableColumnWidth>? widths)
    {
        Dictionary<(string Table, string Column), double> result = [];
        foreach (TableColumnWidth width in widths ?? [])
        {
            if (string.IsNullOrEmpty(width.Table) || string.IsNullOrEmpty(width.Column)) continue;
            if (!IsUsableWidth(width.Pixels)) continue;
            result[(width.Table, width.Column)] = width.Pixels;
        }

        return result;
    }

    private static bool IsUsableWidth(double pixels) =>
        !double.IsNaN(pixels) && !double.IsInfinity(pixels) && pixels > 0;

    private bool WidthsEqual(Dictionary<(string Table, string Column), double> stored) =>
        stored.Count == _widths.Count &&
        stored.All(width =>
            _widths.TryGetValue(width.Key, out double current) &&
            Math.Abs(current - width.Value) < WidthTolerance);

    private static string GetKey(string tableId, string columnKey) => tableId + "/" + columnKey;

    private sealed class RevealScope(
        TableColumnsService service,
        string tableId,
        string key,
        bool restoreHidden) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            if (restoreHidden) service.Restore(tableId, key);
        }
    }
}
