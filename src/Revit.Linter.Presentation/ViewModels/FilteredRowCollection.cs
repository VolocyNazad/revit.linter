using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using Revit.Linter.Presentation.Abstractions.Services;
using Revit.Linter.Presentation.Models;

namespace Revit.Linter.Presentation.ViewModels;

/// <summary>
/// Owns the WPF collection view, its sort order and the filter subscriptions of one list pane.
/// </summary>
/// <typeparam name="TItem">The row type the view displays.</typeparam>
/// <remarks>
/// The view model keeps its bindable properties and supplies only the row predicate and sort order.
/// This type subscribes to every filter group, refreshes the view when a filter changes and stores the
/// subscription state, so each pane does not repeat that plumbing. When a table identifier and a table
/// state service are given, the sort order the user applies is saved and restored.
/// </remarks>
public sealed class FilteredRowCollection<TItem> : IDisposable
    where TItem : class
{
    private readonly Func<TItem, bool> _isVisible;
    private readonly IReadOnlyList<SortDescription> _sortDescriptions;
    private readonly Action<string, IDiagnosticFilter<TItem>>? _filterChanged;
    private readonly Dictionary<string, IReadOnlyList<IDiagnosticFilter<TItem>>> _filterGroups = new(StringComparer.Ordinal);
    private readonly string? _tableId;
    private readonly ITableStateService? _tableState;
    private readonly IReadOnlyList<TableSortColumn> _sortColumns;
    private readonly Dictionary<string, string> _columnByMember;
    private CollectionViewSource? _viewSource;
    private ICollectionView? _view;
    private bool _isApplyingSort;

    /// <summary>Initializes the collection view owner.</summary>
    /// <param name="sortDescriptions">The sort order applied to the view.</param>
    /// <param name="isVisible">Checks whether a row passes the current search and filters.</param>
    /// <param name="filterChanged">Notifies the view model that the user toggled a filter of a group.</param>
    /// <param name="tableId">The table identifier whose sort order is remembered, or <see langword="null"/> to remember nothing.</param>
    /// <param name="sortColumns">Maps the column keys of the table to the properties the view sorts by.</param>
    /// <param name="tableState">Remembers and restores the sort order of the table.</param>
    public FilteredRowCollection(
        IEnumerable<SortDescription> sortDescriptions,
        Func<TItem, bool> isVisible,
        Action<string, IDiagnosticFilter<TItem>>? filterChanged = null,
        string? tableId = null,
        IReadOnlyList<TableSortColumn>? sortColumns = null,
        ITableStateService? tableState = null)
    {
        _sortDescriptions = sortDescriptions.ToArray();
        _isVisible = isVisible ?? throw new ArgumentNullException(nameof(isVisible));
        _filterChanged = filterChanged;
        _tableId = tableId;
        _tableState = tableState;
        _sortColumns = sortColumns ?? [];
        _columnByMember = _sortColumns.ToDictionary(
            column => column.MemberPath, column => column.ColumnKey, StringComparer.Ordinal);
    }

    /// <summary>Gets the view source the pane binds to.</summary>
    public CollectionViewSource? ViewSource => _viewSource;

    /// <summary>Attaches the view source to a collection, replacing the source of a previous collection.</summary>
    /// <param name="source">The rows to display.</param>
    public void Attach(ObservableCollection<TItem> source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        DetachView();

        _viewSource = new CollectionViewSource { Source = source };
        _viewSource.Filter += ViewSource_Filter;
        _view = _viewSource.View;
        ((INotifyCollectionChanged)_view.SortDescriptions).CollectionChanged += SortDescriptions_CollectionChanged;
        ApplySort(_sortDescriptions);
        RestoreSort();
    }

    /// <summary>Refreshes the view after the search text or a row changed.</summary>
    public void Refresh() => _viewSource?.View.Refresh();

    /// <summary>Restores the pane's default sort order, discarding a sort the user applied.</summary>
    /// <remarks>
    /// A column header click replaces the view's sort descriptions, so a guided session that needs a
    /// specific row on top resets the order to the one this collection was created with. The reset is not
    /// stored: it lasts for the session, while the user's saved order stays for the next start.
    /// </remarks>
    public void ResetSort()
    {
        if (_viewSource is null) return;

        ApplySort(_sortDescriptions);
    }

    /// <summary>Replaces one named filter group and refreshes the view.</summary>
    /// <param name="group">The group name, which keeps independent filter sets apart.</param>
    /// <param name="filters">The new filters; an empty sequence removes the group.</param>
    public void SetFilters(string group, IEnumerable<IDiagnosticFilter<TItem>>? filters)
    {
        if (group is null) throw new ArgumentNullException(nameof(group));

        if (_filterGroups.TryGetValue(group, out IReadOnlyList<IDiagnosticFilter<TItem>>? previous))
        {
            foreach (IDiagnosticFilter<TItem> filter in previous)
                filter.PropertyChanged -= Filter_PropertyChanged;
            _filterGroups.Remove(group);
        }

        if (filters is not null)
        {
            IDiagnosticFilter<TItem>[] current = filters.ToArray();
            if (current.Length > 0)
            {
                _filterGroups[group] = current;
                foreach (IDiagnosticFilter<TItem> filter in current)
                    filter.PropertyChanged += Filter_PropertyChanged;
            }
        }

        Refresh();
    }

    /// <summary>Removes every filter group and refreshes the view.</summary>
    public void ClearFilters()
    {
        foreach (IReadOnlyList<IDiagnosticFilter<TItem>> filters in _filterGroups.Values)
            foreach (IDiagnosticFilter<TItem> filter in filters)
                filter.PropertyChanged -= Filter_PropertyChanged;

        _filterGroups.Clear();
        Refresh();
    }

    /// <summary>Releases the view and every filter subscription.</summary>
    public void Dispose()
    {
        ClearFilters();
        DetachView();
    }

    private void DetachView()
    {
        if (_viewSource is null) return;

        if (_view is not null)
            ((INotifyCollectionChanged)_view.SortDescriptions).CollectionChanged -= SortDescriptions_CollectionChanged;
        _viewSource.Filter -= ViewSource_Filter;
        _viewSource = null;
        _view = null;
    }

    private void ApplySort(IEnumerable<SortDescription> sortDescriptions)
    {
        if (_view is null) return;

        _isApplyingSort = true;
        try
        {
            SortDescriptionCollection target = _view.SortDescriptions;
            target.Clear();
            foreach (SortDescription sortDescription in sortDescriptions)
                target.Add(sortDescription);
        }
        finally
        {
            _isApplyingSort = false;
        }
    }

    private void RestoreSort()
    {
        if (_tableState is null || _tableId is null) return;

        TableSortPreference? saved = _tableState.GetSort(_tableId);
        if (saved is null) return;

        TableSortColumn? column = _sortColumns.FirstOrDefault(
            candidate => string.Equals(candidate.ColumnKey, saved.Column, StringComparison.Ordinal));
        if (column is null) return;

        ApplySort([new SortDescription(
            column.MemberPath,
            saved.Descending ? ListSortDirection.Descending : ListSortDirection.Ascending)]);
    }

    private void SortDescriptions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (_isApplyingSort || _tableState is null || _tableId is null || _view is null) return;

        // A header click clears the collection and adds the new description, so an empty collection is
        // an intermediate state rather than a choice to forget the saved order.
        if (_view.SortDescriptions.Count == 0) return;
        SortDescription sort = _view.SortDescriptions[0];
        if (string.IsNullOrEmpty(sort.PropertyName)) return;
        if (!_columnByMember.TryGetValue(sort.PropertyName, out string? columnKey)) return;

        _tableState.SetSort(_tableId, columnKey, sort.Direction);
    }

    private void ViewSource_Filter(object sender, FilterEventArgs args)
        => args.Accepted = args.Item is TItem item && _isVisible(item);

    private void Filter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Refresh();
        if (sender is not IDiagnosticFilter<TItem> filter) return;

        string? group = _filterGroups
            .FirstOrDefault(pair => pair.Value.Contains(filter))
            .Key;
        if (group is not null)
            _filterChanged?.Invoke(group, filter);
    }
}
