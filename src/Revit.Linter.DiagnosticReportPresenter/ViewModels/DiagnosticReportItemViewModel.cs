using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.ReportMessaging;
using Revit.Linter.Localization;
using System.Globalization;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels;

internal sealed partial class DiagnosticReportItemViewModel : ObservableObject
{
    public required DiagnosticSeverity Severity { get; init; }
    public string SeverityText => DiagnosticSeverityLocalizations.GetString(Severity.ToString());
    public required string Code { get; init; }
    public required string DocumentTitle { get; init; }
    public required object? Target { get; init; }
    public required object[]? TargetDependencies { get; init; }
    public required ElementId? TargetElementId { get; init; }
    public required ElementId[] TargetDependencyElementIds { get; init; }
    public required bool IsObsolete { get; init; }
    public required string ObsoleteDescription { get; init; }
    public required string Template { get; init; }
    public required Dictionary<string, object> Args { get; init; }
    public required Action<ElementId> AccentElementDelegate { get; init; }
    public required Func<IEnumerable<FixViewModel>?> FixesFactory { get; init; }
    public required Func<IReadOnlyList<VisualizationPipelineViewModel>> VisualizationPipelinesFactory { get; init; }
    public required Action OpenDocumentationDelegate { get; init; }

    // Fixes and visualizations are built on first use rather than with the row. A report can hold tens
    // of thousands of rows, each fix owns an icon control and several delegates, and only the rows the
    // list actually shows, or the user acts on, ever need them.
    private bool _fixesCreated;
    private IEnumerable<FixViewModel>? _fixes;
    private IReadOnlyList<VisualizationPipelineViewModel>? _visualizationPipelines;

    public IEnumerable<FixViewModel>? Fixes
    {
        get
        {
            if (!_fixesCreated)
            {
                try
                {
                    _fixes = FixesFactory();
                }
                catch (Autodesk.Revit.Exceptions.InvalidObjectException)
                {
                    // The report target was deleted or undone after the row was created. A row
                    // without fixes stays visible instead of breaking the bound list.
                    _fixes = [];
                }
                _fixesCreated = true;
            }

            return _fixes;
        }
    }

    public IReadOnlyList<VisualizationPipelineViewModel> VisualizationPipelines
        => _visualizationPipelines ??= CreateVisualizationPipelinesSafe();

    private IReadOnlyList<VisualizationPipelineViewModel> CreateVisualizationPipelinesSafe()
    {
        try
        {
            return VisualizationPipelinesFactory();
        }
        catch (Autodesk.Revit.Exceptions.InvalidObjectException)
        {
            // Same stale-target race as for fixes: the row stays visible without visualizations.
            return [];
        }
    }
    public required DateTime Created { get; init; }
    public string CreatedText => Created.Humanize(utcDate: false, culture: CultureInfo.CurrentUICulture);
    public required string ShowElementToolTipFormat { get; init; }

    public bool HasFixes => Fixes?.Any() == true;
    public bool HasVisualizationPipelines => VisualizationPipelines.Count > 0;

    [ObservableProperty]
    private bool _hasActiveVisualization;

    /// <summary>
    /// Marks the pipelines matching the currently applied visualization, if any.
    /// </summary>
    /// <remarks>
    /// The report view-model calls this for every row after the active visualization changes;
    /// rows whose pipelines were never built keep no state.
    /// </remarks>
    public void RefreshVisualizationState(
        IElementVisualizationPipeline? activePipeline, ElementId? activeTargetId)
    {
        bool hasActive = false;
        if (_visualizationPipelines is not null)
        {
            foreach (VisualizationPipelineViewModel pipeline in _visualizationPipelines)
            {
                bool isActive = activePipeline is not null
                    && ReferenceEquals(activePipeline, pipeline.Pipeline)
                    && activeTargetId?.Equals(TargetElementId) == true;
                pipeline.IsActive = isActive;
                hasActive |= isActive;
            }
        }

        HasActiveVisualization = hasActive;
    }

    private ReportMessage? _message;
    private ReportMessage Message => _message ??= ReportMessageParser.Parse(
        Template,
        Args,
        static value => ReportLinks.TryGetLinks<ElementId>(value, static id => id.ToString()));

    public IReadOnlyList<ReportTextPart> MessageParts => Message.Parts;
    public string MessageText => Message.Text;

    [RelayCommand]
    private void AccentElement(object? parameter)
    {
        if (parameter is not ElementId elementId) return;
        AccentElementDelegate(elementId);
    }

    [RelayCommand]
    private void OpenDocumentation() => OpenDocumentationDelegate();

    [RelayCommand(CanExecute = nameof(HasVisualizationPipelines))]
    private async Task ShowVisualization(CancellationToken cancellationToken)
    {
        if (VisualizationPipelines.Count > 0)
            await VisualizationPipelines[0].Show(cancellationToken);
    }
}

internal sealed partial class VisualizationPipelineViewModel : ObservableObject
{
    public required string Title { get; init; }
    public required IElementVisualizationPipeline Pipeline { get; init; }
    public required Func<bool, CancellationToken, Task> ShowDelegate { get; init; }

    [ObservableProperty]
    private bool _isActive;

    [RelayCommand]
    public async Task Show(CancellationToken cancellationToken)
        => await ShowDelegate(false, cancellationToken);

    [RelayCommand]
    private async Task ShowFromMenu(CancellationToken cancellationToken)
        => await ShowDelegate(true, cancellationToken);
}

internal sealed partial class FixViewModel
{
    public required object Icon { get; init; }
    public required string Title { get; init; }
    public required Func<CancellationToken, Task> FixDelegate { get; init; }

    [RelayCommand]
    private async Task Fix(CancellationToken cancellationToken)
        => await FixDelegate(cancellationToken);
}
