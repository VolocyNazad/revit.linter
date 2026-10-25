using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions.Models;
using Revit.Linter.PracticalTourPresenter.Infrastructure.Services;
using Revit.Linter.Localization;

namespace Revit.Linter.PracticalTourPresenter.ViewModels;

/// <summary>
/// Presents the current practical-tour step, the visible step list and the commands the tour pane offers.
/// </summary>
[GenerateLocalizedProperties]
internal sealed partial class PracticalTourViewModel(PracticalTourStateMachine stateMachine) : ObservableObject
{

    private static readonly PracticalTourStep[] VisibleSteps =
    [
        PracticalTourStep.OpenDocument,
        PracticalTourStep.OpenConfigurationFolder,
        PracticalTourStep.SearchAndFilters,
        PracticalTourStep.SelectDiagnostic,
        PracticalTourStep.RunDiagnostics,
        PracticalTourStep.InspectFinding,
        PracticalTourStep.OpenHelp,
        PracticalTourStep.ShowElement,
        PracticalTourStep.SelectVisualization,
        PracticalTourStep.NavigateFindings,
        PracticalTourStep.UnderstandFix,
        PracticalTourStep.FixList,
        PracticalTourStep.UnderstandIgnore,
        PracticalTourStep.IgnoreList,
        PracticalTourStep.ExportReport,
        PracticalTourStep.ExportReportAnotherFormat,
    ];

    private bool _tutorialSampleQueued;
    private IReadOnlyList<PracticalTourStepRow> _steps = [];

    public PracticalTourStep CurrentStep => stateMachine.CurrentStep;
    public bool HasCleanRun => stateMachine.HasCleanRun;
    public bool HasSingleVisualization => stateMachine.HasSingleVisualization;
    public string? FirstExportFormat => stateMachine.FirstExportFormat;
    public IReadOnlyList<PracticalTourStepRow> Steps => _steps;

    public string Instruction => CurrentStep switch
    {
        PracticalTourStep.OpenDocument when _tutorialSampleQueued => OpenTutorialSampleText,
        PracticalTourStep.OpenDocument => OpenDocumentText,
        PracticalTourStep.OpenConfigurationFolder => OpenConfigurationFolderText,
        PracticalTourStep.SearchAndFilters => SearchAndFiltersText,
        PracticalTourStep.SelectDiagnostic => SelectDiagnosticText,
        PracticalTourStep.RunDiagnostics => RunDiagnosticsText,
        PracticalTourStep.InspectFinding => InspectFindingText,
        PracticalTourStep.OpenHelp => OpenHelpText,
        PracticalTourStep.ShowElement => ShowElementText,
        PracticalTourStep.SelectVisualization => SelectVisualizationText,
        PracticalTourStep.NavigateFindings => NavigateFindingsText,
        PracticalTourStep.UnderstandFix => UnderstandFixText,
        PracticalTourStep.UnderstandIgnore => UnderstandIgnoreText,
        PracticalTourStep.IgnoreList => IgnoreListText,
        PracticalTourStep.FixList => FixListText,
        PracticalTourStep.ExportReport when stateMachine.HasCleanRun => CleanRunExportReportText,
        PracticalTourStep.ExportReport => ExportReportText,
        PracticalTourStep.ExportReportAnotherFormat => ExportReportAnotherFormatText,
        _ => CompletedText,
    };

    public event EventHandler? StopRequested;
    public event EventHandler? OptOutRequested;
    public event EventHandler? RestartRequested;

    public void Start(bool hasOpenDocument, PracticalTourProgress? progress, bool tutorialSampleQueued = false)
    {
        _tutorialSampleQueued = tutorialSampleQueued;
        stateMachine.Start(
            hasOpenDocument,
            progress?.CurrentStep,
            progress?.HasCleanRun == true,
            progress?.HasSingleVisualization == true,
            progress?.FirstExportFormat);
        RefreshSteps();
        OnPropertyChanged(nameof(CurrentStep));
        OnPropertyChanged(nameof(Instruction));
    }

    public void Observe(PracticalTourStep completedAction)
    {
        if (!stateMachine.Observe(completedAction)) return;
        RefreshSteps();
        OnPropertyChanged(nameof(CurrentStep));
        OnPropertyChanged(nameof(Instruction));
        if (!stateMachine.IsActive) StopRequested?.Invoke(this, EventArgs.Empty);
    }

    public void ObserveSearchText(string searchText)
    {
        if (!stateMachine.ObserveSearchText(searchText)) return;
        RefreshSteps();
        OnPropertyChanged(nameof(CurrentStep));
        OnPropertyChanged(nameof(Instruction));
    }

    public void ObserveDiagnosticRun(bool hasFindings)
    {
        if (!stateMachine.ObserveDiagnosticRun(hasFindings)) return;
        RefreshSteps();
        OnPropertyChanged(nameof(CurrentStep));
        OnPropertyChanged(nameof(Instruction));
    }

    public void ObserveVisualization(bool selectedFromMenu, int availableOptionCount)
    {
        if (!stateMachine.ObserveVisualization(selectedFromMenu, availableOptionCount)) return;
        RefreshSteps();
        OnPropertyChanged(nameof(CurrentStep));
        OnPropertyChanged(nameof(Instruction));
        OnPropertyChanged(nameof(HasSingleVisualization));
    }

    public void ObserveReportExport(string format)
    {
        if (!stateMachine.ObserveReportExport(format)) return;
        RefreshSteps();
        OnPropertyChanged(nameof(CurrentStep));
        OnPropertyChanged(nameof(Instruction));
        if (!stateMachine.IsActive) StopRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshSteps()
    {
        _steps = VisibleSteps.Select(CreateStepRow).ToArray();
        OnPropertyChanged(nameof(Steps));
    }

    private PracticalTourStepRow CreateStepRow(PracticalTourStep step)
    {
        bool isUnavailable = stateMachine.HasCleanRun
                             && step is PracticalTourStep.InspectFinding
                                 or PracticalTourStep.OpenHelp
                                 or PracticalTourStep.ShowElement
                                 or PracticalTourStep.SelectVisualization
                                 or PracticalTourStep.NavigateFindings
                                 or PracticalTourStep.UnderstandFix
                                 or PracticalTourStep.UnderstandIgnore
                                 or PracticalTourStep.IgnoreList;
        isUnavailable |= stateMachine.HasSingleVisualization
                         && step == PracticalTourStep.SelectVisualization;
        int currentIndex = Array.IndexOf(VisibleSteps, CurrentStep);
        int stepIndex = Array.IndexOf(VisibleSteps, step);
        return new(
            GetStepTitle(step),
            GetStepInstruction(step),
            step == CurrentStep,
            !isUnavailable && (CurrentStep == PracticalTourStep.Completed || stepIndex < currentIndex),
            isUnavailable);
    }

    private string GetStepTitle(PracticalTourStep step) => step switch
    {
        PracticalTourStep.OpenDocument => OpenDocumentStepText,
        PracticalTourStep.OpenConfigurationFolder => OpenConfigurationFolderStepText,
        PracticalTourStep.SearchAndFilters => SearchAndFiltersStepText,
        PracticalTourStep.SelectDiagnostic => SelectDiagnosticStepText,
        PracticalTourStep.RunDiagnostics => RunDiagnosticsStepText,
        PracticalTourStep.InspectFinding => InspectFindingStepText,
        PracticalTourStep.OpenHelp => OpenHelpStepText,
        PracticalTourStep.ShowElement => ShowElementStepText,
        PracticalTourStep.SelectVisualization when stateMachine.HasSingleVisualization
            => SelectVisualizationUnavailableStepText,
        PracticalTourStep.SelectVisualization => SelectVisualizationStepText,
        PracticalTourStep.NavigateFindings => NavigateFindingsStepText,
        PracticalTourStep.UnderstandFix => InspectFixesStepText,
        PracticalTourStep.UnderstandIgnore => UnderstandIgnoreStepText,
        PracticalTourStep.IgnoreList => IgnoreListStepText,
        PracticalTourStep.FixList => FixListStepText,
        PracticalTourStep.ExportReport => ExportReportStepText,
        PracticalTourStep.ExportReportAnotherFormat => ExportReportAnotherFormatStepText,
        _ => string.Empty,
    };

    private string GetStepInstruction(PracticalTourStep step) => step switch
    {
        PracticalTourStep.OpenDocument when _tutorialSampleQueued => OpenTutorialSampleText,
        PracticalTourStep.OpenDocument => OpenDocumentText,
        PracticalTourStep.OpenConfigurationFolder => OpenConfigurationFolderText,
        PracticalTourStep.SearchAndFilters => SearchAndFiltersText,
        PracticalTourStep.SelectDiagnostic => SelectDiagnosticText,
        PracticalTourStep.RunDiagnostics => RunDiagnosticsText,
        PracticalTourStep.InspectFinding => InspectFindingText,
        PracticalTourStep.OpenHelp => OpenHelpText,
        PracticalTourStep.ShowElement => ShowElementText,
        PracticalTourStep.SelectVisualization => SelectVisualizationText,
        PracticalTourStep.NavigateFindings => NavigateFindingsText,
        PracticalTourStep.UnderstandFix => UnderstandFixText,
        PracticalTourStep.UnderstandIgnore => UnderstandIgnoreText,
        PracticalTourStep.IgnoreList => IgnoreListText,
        PracticalTourStep.FixList => FixListText,
        PracticalTourStep.ExportReport when stateMachine.HasCleanRun => CleanRunExportReportText,
        PracticalTourStep.ExportReport => ExportReportText,
        PracticalTourStep.ExportReportAnotherFormat => ExportReportAnotherFormatText,
        _ => string.Empty,
    };

    [RelayCommand]
    private void Stop()
    {
        StopRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void OptOut()
    {
        OptOutRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Restart()
    {
        RestartRequested?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>Describes one visible row of the practical-tour step list.</summary>
internal sealed record PracticalTourStepRow(
    string Title,
    string Instruction,
    bool IsCurrent,
    bool IsCompleted,
    bool IsUnavailable);
