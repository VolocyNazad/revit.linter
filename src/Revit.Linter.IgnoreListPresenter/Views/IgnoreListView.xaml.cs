using Revit.Linter.Behaviors;
using Revit.Linter.IgnoreListPresenter.ViewModels;
using Revit.Linter.ThemeManaging.Abstractions.Services;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace Revit.Linter.IgnoreListPresenter.Views;

/// <summary>
/// Displays the ignore list of the active document.
/// </summary>
public sealed partial class IgnoreListView
{
    // The practical tour highlights the first training entry; the code matches the
    // tour-specific highlights in the other panes.
    private const string TourDiagnosticCode = "TOUR001";
    private const string IgnoreListHighlightKey = "IgnoreList";

    private INotifyCollectionChanged? _observedEntriesView;

    /// <summary>
    /// Initializes the ignore-list view and registers it for application theme updates.
    /// </summary>
    /// <param name="themeService">Applies the current application theme to the view.</param>
    public IgnoreListView(IThemeService themeService)
    {
        InitializeComponent();
        themeService.Register(this);
        DataContextChanged += View_DataContextChanged;
        EntriesGrid.Loaded += EntriesGrid_Loaded;
        EntriesGrid.Unloaded += EntriesGrid_Unloaded;
    }

    private void View_DataContextChanged(object sender, DependencyPropertyChangedEventArgs args)
        => SubscribeToEntriesView();

    private void EntriesGrid_Loaded(object sender, RoutedEventArgs args)
    {
        SubscribeToEntriesView();
        EvaluateRealizedRows();
    }

    private void EntriesGrid_Unloaded(object sender, RoutedEventArgs args) => UnsubscribeFromEntriesView();

    private void EntriesGrid_LoadingRow(object? sender, DataGridRowEventArgs args) => EvaluateRealizedRows();

    private void EntriesGrid_UnloadingRow(object? sender, DataGridRowEventArgs args)
        => OnboardingHighlight.SetKey(args.Row, null);

    private void SubscribeToEntriesView()
    {
        INotifyCollectionChanged? view = (DataContext as IgnoreListViewModel)
            ?.CollectionViewSource?.View as INotifyCollectionChanged;
        if (ReferenceEquals(view, _observedEntriesView)) return;
        UnsubscribeFromEntriesView();
        _observedEntriesView = view;
        if (_observedEntriesView is not null)
            _observedEntriesView.CollectionChanged += OnEntriesViewChanged;
    }

    private void UnsubscribeFromEntriesView()
    {
        if (_observedEntriesView is null) return;
        _observedEntriesView.CollectionChanged -= OnEntriesViewChanged;
        _observedEntriesView = null;
    }

    private void OnEntriesViewChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(EvaluateRealizedRows);
            return;
        }

        EvaluateRealizedRows();
    }

    private void EvaluateRealizedRows() => FirstMatchRowHighlight.Refresh(
        EntriesGrid,
        IsTourEntry,
        IgnoreListHighlightKey);

    private static bool IsTourEntry(object? item) =>
        item is IgnoreListItemViewModel entry
        && string.Equals(entry.Code, TourDiagnosticCode, StringComparison.Ordinal);
}
