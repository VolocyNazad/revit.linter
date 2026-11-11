using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Revit.Linter.Presentation.Abstractions.Models;
using Revit.Linter.Presentation.Abstractions.Services;
using Revit.Linter.Presentation.Models;
using System.Collections.ObjectModel;

namespace Revit.Linter.Presentation.ViewModels;

/// <summary>
/// Presents the visibility popup of one table and keeps it in step with the stored choice.
/// </summary>
/// <remarks>
/// The table must keep at least one data column visible, so the last visible column cannot be hidden.
/// Hiding a column never changes the current sort: the table keeps its sort order when that column is
/// hidden and applies it again when the column returns.
/// </remarks>
public sealed partial class TableColumnsViewModel : ObservableObject, IDisposable
{
    private readonly ITableColumnsService _service;
    private bool _syncing;
    private int _disposed;

    /// <summary>Initializes the popup from the current visibility and starts observing changes.</summary>
    /// <param name="tableId">The table identifier, one of <see cref="TableColumnIds"/>.</param>
    /// <param name="service">Remembers and publishes column visibility.</param>
    /// <param name="columns">The hideable columns in display order.</param>
    public TableColumnsViewModel(
        string tableId,
        ITableColumnsService service,
        IEnumerable<TableColumnDefinition> columns)
    {
        if (string.IsNullOrEmpty(tableId)) throw new ArgumentException("A table identifier is required.", nameof(tableId));
        TableId = tableId;
        _service = service ?? throw new ArgumentNullException(nameof(service));
        Columns = new ObservableCollection<TableColumnViewModel>(
            (columns ?? throw new ArgumentNullException(nameof(columns))).Select(column => new TableColumnViewModel(
                column.Key,
                column.Title,
                service.IsVisible(tableId, column.Key),
                OnColumnVisibilityChanged)));

        _service.Changed += Service_Changed;
        ShowAllWhenEverythingIsHidden();
        Sync();
    }

    /// <summary>Raised after a change that this popup did not make, so the table can re-apply visibility.</summary>
    public event EventHandler? VisibilityChanged;

    /// <summary>Gets the table identifier.</summary>
    public string TableId { get; }

    /// <summary>Gets the hideable columns in display order.</summary>
    public ObservableCollection<TableColumnViewModel> Columns { get; }

    /// <summary>Checks whether a column is currently visible.</summary>
    /// <param name="columnKey">The column key, one of <see cref="TableColumnKeys"/>.</param>
    /// <returns><see langword="true"/> when the table shows the column.</returns>
    public bool IsVisible(string columnKey) => _service.IsVisible(TableId, columnKey);

    /// <summary>Shows every column of the table.</summary>
    [RelayCommand]
    private void ShowAll() => _service.ShowAll(TableId);

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _service.Changed -= Service_Changed;
    }

    private void OnColumnVisibilityChanged(TableColumnViewModel column, bool isVisible)
    {
        if (_syncing) return;

        // The last visible data column stays: a table without any column would show only its action icons.
        if (!isVisible && Columns.All(item => !item.IsVisible))
        {
            Sync();
            return;
        }

        _service.SetVisibility(TableId, column.Key, isVisible);
        Sync();
    }

    private void Service_Changed(object? sender, TableColumnsChangedEventArgs args)
    {
        if (args.TableId is not null && !string.Equals(args.TableId, TableId, StringComparison.Ordinal)) return;
        Sync();
        VisibilityChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ShowAllWhenEverythingIsHidden()
    {
        // A hand-edited or older settings file must not leave a table without any data column.
        if (Columns.Count > 0 && Columns.All(column => !column.IsVisible))
            _service.ShowAll(TableId);
    }

    private void Sync()
    {
        _syncing = true;
        try
        {
            foreach (TableColumnViewModel column in Columns)
                column.ApplyVisibility(_service.IsVisible(TableId, column.Key));
        }
        finally
        {
            _syncing = false;
        }
    }
}
