using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Revit.Linter.DiagnosticReportPresenter.Infrastructure;

/// <summary>
/// An observable collection that can take many items with a single change notification.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
internal sealed class BatchObservableCollection<T> : ObservableCollection<T>
{
    public BatchObservableCollection()
    {
    }

    public BatchObservableCollection(IEnumerable<T> items) : base(items)
    {
    }

    /// <summary>
    /// Appends the items and raises one <see cref="NotifyCollectionChangedAction.Reset"/> notification.
    /// </summary>
    /// <remarks>
    /// A bound collection view re-reads the whole collection on a reset, so this pays off for large
    /// batches and is wasteful for a few items.
    /// </remarks>
    public void AddRange(IReadOnlyCollection<T> items)
    {
        if (items.Count == 0) return;

        CheckReentrancy();
        foreach (T item in items)
            Items.Add(item);

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}