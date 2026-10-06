using Revit.Linter.Behaviors;
using Revit.Linter.OpenedDocuments.ViewModels;
using Revit.Linter.ThemeManaging.Abstractions.Services;
using Revit.Linter.DiagnosticReportPresenter.ViewModels;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace Revit.Linter.DiagnosticReportPresenter.Views;

public sealed partial class DiagnosticReportView
{
    // The practical tour highlights the first training finding; the code matches the
    // tour-specific checkbox highlight in the diagnostics pane.
    private const string TourDiagnosticCode = "TOUR001";
    private const string InspectFindingHighlightKey = "InspectFinding";

    private INotifyCollectionChanged? _observedFindingsView;

    /// <summary>
    /// Initializes a new diagnostic report view.
    /// </summary>
    /// <param name="openedDocumentsViewModel">The opened-document selector displayed by the view.</param>
    /// <param name="themeService">The service that applies and updates the application theme.</param>
    public DiagnosticReportView(
        OpenedDocumentsViewModel openedDocumentsViewModel,
        IThemeService themeService)
    {
        OpenedDocumentsViewModel = openedDocumentsViewModel;
        InitializeComponent();
        themeService.Register(this);
        DataContextChanged += View_DataContextChanged;
        FindingsGrid.Loaded += FindingsGrid_Loaded;
        FindingsGrid.Unloaded += FindingsGrid_Unloaded;
    }

    /// <summary>
    /// Gets the opened-document selector displayed by the view.
    /// </summary>
    public OpenedDocumentsViewModel OpenedDocumentsViewModel { get; }

    private void FixButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: DiagnosticReportItemViewModel item } button)
            return;

        FixViewModel[] fixes = item.Fixes?.ToArray() ?? [];
        if (fixes.Length == 1)
        {
            fixes[0].FixCommand.Execute(null);
            return;
        }

        if (fixes.Length > 1 && button.ContextMenu is not null)
        {
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
        }
    }

    private void View_DataContextChanged(object sender, DependencyPropertyChangedEventArgs args)
        => SubscribeToFindingsView();

    private void FindingsGrid_Loaded(object sender, RoutedEventArgs args)
    {
        SubscribeToFindingsView();
        EvaluateRealizedRows();
    }

    private void FindingsGrid_Unloaded(object sender, RoutedEventArgs args) => UnsubscribeFromFindingsView();

    private void FindingsGrid_LoadingRow(object? sender, DataGridRowEventArgs args) => EvaluateRealizedRows();

    private void FindingsGrid_UnloadingRow(object? sender, DataGridRowEventArgs args)
        => OnboardingHighlight.SetKey(args.Row, null);

    private void SubscribeToFindingsView()
    {
        INotifyCollectionChanged? view = (DataContext as DiagnosticReportViewModel)
            ?.CollectionViewSource?.View as INotifyCollectionChanged;
        if (ReferenceEquals(view, _observedFindingsView)) return;
        UnsubscribeFromFindingsView();
        _observedFindingsView = view;
        if (_observedFindingsView is not null)
            _observedFindingsView.CollectionChanged += OnFindingsViewChanged;
    }

    private void UnsubscribeFromFindingsView()
    {
        if (_observedFindingsView is null) return;
        _observedFindingsView.CollectionChanged -= OnFindingsViewChanged;
        _observedFindingsView = null;
    }

    private void OnFindingsViewChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(EvaluateRealizedRows);
            return;
        }

        EvaluateRealizedRows();
    }

    private void EvaluateRealizedRows() => FirstMatchRowHighlight.Refresh(
        FindingsGrid,
        IsTourFinding,
        InspectFindingHighlightKey);

    private static bool IsTourFinding(object? item) =>
        item is DiagnosticReportItemViewModel report
        && string.Equals(report.Code, TourDiagnosticCode, StringComparison.Ordinal);

    private void VisualizationButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: DiagnosticReportItemViewModel item } button)
            return;

        // The row becomes the current one, so that stepping to the next finding continues from it.
        (DataContext as DiagnosticReportViewModel)?.CollectionViewSource?.View.MoveCurrentTo(item);

        if (item.VisualizationPipelines.Count == 1)
        {
            item.ShowVisualizationCommand.Execute(null);
            return;
        }

        if (item.VisualizationPipelines.Count > 1 && button.ContextMenu is not null)
        {
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
        }
    }
}
