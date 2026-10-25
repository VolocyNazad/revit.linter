using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace Revit.Linter.Presentation.ViewModels;

/// <summary>
/// Owns the WPF collection view, its sort order and the filter subscriptions of one list pane.
/// </summary>
/// <typeparam name="TItem">The row type the view displays.</typeparam>
/// <remarks>
/// The view model keeps its bindable properties and supplies only the row predicate and sort order.
/// This type subscribes to every filter group, refreshes the view when a filter changes and stores the
/// subscription state, so each pane does not repeat that plumbing.
/// </remarks>
public sealed class FilteredRowCollection<TItem> : IDisposable
    where TItem : class
{
    private readonly Func<TItem, bool> _isVisible;
    private readonly IReadOnlyList<SortDescription> _sortDescriptions;
    private readonly Action? _filterChanged;
    private readonly Dictionary<string, IReadOnlyList<IDiagnosticFilter<TItem>>> _filterGroups = new(StringComparer.Ordinal);
    private CollectionViewSource? _viewSource;

    /// <summary>Initializes the collection view owner.</summary>
    /// <param name="sortDescriptions">The sort order applied to the view.</param>
    /// <param name="isVisible">Checks whether a row passes the current search and filters.</param>
    /// <param name="filterChanged">Notifies the view model that the user toggled a filter.</param>
    public FilteredRowCollection(
        IEnumerable<SortDescription> sortDescriptions,
        Func<TItem, bool> isVisible,
        Action? filterChanged = null)
    {
        _sortDescriptions = sortDescriptions.ToArray();
        _isVisible = isVisible ?? throw new ArgumentNullException(nameof(isVisible));
        _filterChanged = filterChanged;
    }

    /// <summary>Gets the view source the pane binds to.</summary>
    public CollectionViewSource? ViewSource => _viewSource;

    /// <summary>Attaches the view source to a collection, replacing the source of a previous collection.</summary>
    /// <param name="source">The rows to display.</param>
    public void Attach(ObservableCollection<TItem> source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        if (_viewSource is not null)
            _viewSource.Filter -= ViewSource_Filter;

        _viewSource = new CollectionViewSource { Source = source };
        _viewSource.Filter += ViewSource_Filter;
        _viewSource.SortDescriptions.Clear();
        foreach (SortDescription sortDescription in _sortDescriptions)
            _viewSource.SortDescriptions.Add(sortDescription);
    }

    /// <summary>Refreshes the view after the search text or a row changed.</summary>
    public void Refresh() => _viewSource?.View.Refresh();

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
        if (_viewSource is not null)
        {
            _viewSource.Filter -= ViewSource_Filter;
            _viewSource = null;
        }
    }

    private void ViewSource_Filter(object sender, FilterEventArgs args)
        => args.Accepted = args.Item is TItem item && _isVisible(item);

    private void Filter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Refresh();
        _filterChanged?.Invoke();
    }
}
