using Revit.Linter.Presentation.Abstractions.Models;
using Revit.Linter.Presentation.Abstractions.Services;
using Revit.Linter.Presentation.Models;
using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.Presentation.Services;

/// <summary>
/// Keeps the hidden table columns in memory and persists them to <c>columns.yml</c>.
/// </summary>
/// <remarks>
/// A column is visible unless its <c>table/column</c> key is present, so a new column needs no migration.
/// The reveal scope of a guided action changes only the in-memory state, never the stored preference.
/// </remarks>
public sealed class TableColumnsService : ITableColumnsService, IDisposable
{
    private readonly IValueStore<TableColumnsSettings> _store;
    private readonly HashSet<string> _hidden;
    private readonly IDisposable _subscription;
    private int _disposed;

    /// <summary>Initializes the service from the saved settings.</summary>
    /// <param name="store">Persists the hidden columns.</param>
    public TableColumnsService(IValueStore<TableColumnsSettings> store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _hidden = new HashSet<string>(store.CurrentValue.Hidden ?? [], StringComparer.Ordinal);
        _subscription = store.OnChange(ApplyStoredSettings);
    }

    /// <inheritdoc />
    public event EventHandler<TableColumnsChangedEventArgs>? Changed;

    /// <inheritdoc />
    public bool IsVisible(string tableId, string columnKey)
        => !_hidden.Contains(GetKey(tableId, columnKey));

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
        var stored = new HashSet<string>(settings.Hidden ?? [], StringComparer.Ordinal);
        if (stored.SetEquals(_hidden)) return;

        _hidden.Clear();
        _hidden.UnionWith(stored);
        Changed?.Invoke(this, new TableColumnsChangedEventArgs(tableId: null));
    }

    private void Restore(string tableId, string key)
    {
        if (!_hidden.Add(key)) return;
        Changed?.Invoke(this, new TableColumnsChangedEventArgs(tableId));
    }

    private void Persist() =>
        _store.Update(settings => settings.Hidden = _hidden.OrderBy(key => key, StringComparer.Ordinal).ToList());

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
