using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Revit.Linter.Core.Abstractions.Models;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Data;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels;

// Stepping through the findings: the row the table marks as current, its position among the rows the
// table shows, and the commands that move to a neighbouring row and visualize it.
internal sealed partial class DiagnosticReportViewModel
{
    // Name of the visualization shown last. Stepping to another row repeats the same kind of
    // visualization when that row offers it, instead of always taking the first one.
    private string? _preferredVisualizationName;

    /// <summary>
    /// Gets the position of the current row among the rows the table shows, such as "3 / 47".
    /// </summary>
    [ObservableProperty]
    public partial string NavigationPositionText { get; private set; } = string.Empty;

    partial void OnCollectionViewSourceChanged(CollectionViewSource? oldValue, CollectionViewSource? newValue)
    {
        if (oldValue?.View is { } oldView)
        {
            oldView.CurrentChanged -= View_CurrentChanged;
            oldView.CollectionChanged -= View_CollectionChanged;
        }

        if (newValue?.View is { } newView)
        {
            // A new view makes its first row current. Start with no current row instead, so that the
            // first step forward shows the first finding rather than skipping it.
            newView.MoveCurrentToPosition(-1);
            newView.CurrentChanged += View_CurrentChanged;
            newView.CollectionChanged += View_CollectionChanged;
        }

        UpdateNavigationPosition();
    }

    private void View_CurrentChanged(object? sender, EventArgs e) => UpdateNavigationPosition();

    private void View_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => UpdateNavigationPosition();

    private void UpdateNavigationPosition()
    {
        if (CollectionViewSource?.View is not ListCollectionView view)
        {
            NavigationPositionText = string.Empty;
            return;
        }

        int position = view.CurrentPosition;
        string positionText = position >= 0 && position < view.Count
            ? (position + 1).ToString(CultureInfo.CurrentCulture)
            : "\u2013";
        NavigationPositionText = $"{positionText} / {view.Count.ToString(CultureInfo.CurrentCulture)}";
    }

    [RelayCommand]
    private Task ShowNextVisualization() => ShowNeighbourVisualizationAsync(1);

    [RelayCommand]
    private Task ShowPreviousVisualization() => ShowNeighbourVisualizationAsync(-1);

    [RelayCommand]
    private Task ShowCurrentVisualization()
        => CollectionViewSource?.View.CurrentItem is DiagnosticReportItemViewModel item
            ? ShowPreferredVisualizationAsync(item)
            : Task.CompletedTask;

    // Moves from the current row in the order the table shows, skipping rows that cannot be visualized,
    // and stops at either end of the list. Without a current row it starts from the matching end.
    private async Task ShowNeighbourVisualizationAsync(int step)
    {
        if (CollectionViewSource?.View is not ListCollectionView view || view.Count == 0) return;

        int index = view.CurrentPosition;
        if (index < 0 || index >= view.Count)
            index = step > 0 ? -1 : view.Count;

        for (index += step; index >= 0 && index < view.Count; index += step)
        {
            if (view.GetItemAt(index) is not DiagnosticReportItemViewModel { HasVisualizationPipelines: true } item)
                continue;

            view.MoveCurrentToPosition(index);
            await ShowPreferredVisualizationAsync(item);
            _activityStream.Publish(new FindingNavigationUsedActivity(item.Code));
            return;
        }
    }

    private Task ShowPreferredVisualizationAsync(DiagnosticReportItemViewModel item)
    {
        IReadOnlyList<VisualizationPipelineViewModel> pipelines = item.VisualizationPipelines;
        if (pipelines.Count == 0) return Task.CompletedTask;

        VisualizationPipelineViewModel pipeline =
            pipelines.FirstOrDefault(candidate => candidate.Title == _preferredVisualizationName) ?? pipelines[0];
        return pipeline.ShowCommand.ExecuteAsync(null);
    }
}
