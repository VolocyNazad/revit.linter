using Revit.Linter.Behaviors;
using Revit.Linter.FixReportPresenter.ViewModels;
using Revit.Linter.OpenedDocuments.ViewModels;
using Revit.Linter.ThemeManaging.Abstractions.Services;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace Revit.Linter.FixReportPresenter.Views;

/// <summary>
/// Displays fix reports with document filtering and element-link interaction.
/// </summary>
public sealed partial class FixReportView
{
    // The practical tour highlights the first training fix; the code matches the
    // tour-specific highlights in the other panes.
    private const string TourDiagnosticCode = "TOUR001";
    private const string FixListHighlightKey = "FixList";

    private INotifyCollectionChanged? _observedFixesView;

    /// <summary>
    /// Initializes the fix-report view, its opened-document selector, and application theme registration.
    /// </summary>
    /// <param name="openedDocumentsViewModel">Provides the documents currently open in Revit.</param>
    /// <param name="themeService">Applies the current application theme to the view.</param>
    public FixReportView(
        OpenedDocumentsViewModel openedDocumentsViewModel,
        IThemeService themeService)
    {
        OpenedDocumentsViewModel = openedDocumentsViewModel;
        InitializeComponent();
        themeService.Register(this);
        DataContextChanged += View_DataContextChanged;
        FixesGrid.Loaded += FixesGrid_Loaded;
        FixesGrid.Unloaded += FixesGrid_Unloaded;
    }

    /// <summary>
    /// Gets the shared view model used by the document selector.
    /// </summary>
    public OpenedDocumentsViewModel OpenedDocumentsViewModel { get; }

    private void View_DataContextChanged(object sender, DependencyPropertyChangedEventArgs args)
        => SubscribeToFixesView();

    private void FixesGrid_Loaded(object sender, RoutedEventArgs args)
    {
        SubscribeToFixesView();
        EvaluateRealizedRows();
    }

    private void FixesGrid_Unloaded(object sender, RoutedEventArgs args) => UnsubscribeFromFixesView();

    private void FixesGrid_LoadingRow(object? sender, DataGridRowEventArgs args) => EvaluateRealizedRows();

    private void FixesGrid_UnloadingRow(object? sender, DataGridRowEventArgs args)
        => OnboardingHighlight.SetKey(args.Row, null);

    private void SubscribeToFixesView()
    {
        INotifyCollectionChanged? view = (DataContext as FixReportViewModel)
            ?.CollectionViewSource?.View as INotifyCollectionChanged;
        if (ReferenceEquals(view, _observedFixesView)) return;
        UnsubscribeFromFixesView();
        _observedFixesView = view;
        if (_observedFixesView is not null)
            _observedFixesView.CollectionChanged += OnFixesViewChanged;
    }

    private void UnsubscribeFromFixesView()
    {
        if (_observedFixesView is null) return;
        _observedFixesView.CollectionChanged -= OnFixesViewChanged;
        _observedFixesView = null;
    }

    private void OnFixesViewChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(EvaluateRealizedRows);
            return;
        }

        EvaluateRealizedRows();
    }

    private void EvaluateRealizedRows() => FirstMatchRowHighlight.Refresh(
        FixesGrid,
        IsTourFix,
        FixListHighlightKey);

    private static bool IsTourFix(object? item) =>
        item is FixReportItemViewModel fix
        && string.Equals(fix.Code, TourDiagnosticCode, StringComparison.Ordinal);
}
