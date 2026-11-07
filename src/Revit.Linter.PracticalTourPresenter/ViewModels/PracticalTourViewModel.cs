using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions.Models;
using Revit.Linter.PracticalTourPresenter.Infrastructure.Services;
using Revit.Linter.Localization;

namespace Revit.Linter.PracticalTourPresenter.ViewModels;

/// <summary>
/// Presents the current practical-tour step, the visible step list and the commands the tour pane offers.
/// </summary>
/// <remarks>
/// What each step shows lives in <see cref="PracticalTourStepCatalog"/>; this view model only decides which
/// of the catalog texts applies to the current session and how the list is marked up.
/// </remarks>
[GenerateLocalizedProperties]
internal sealed partial class PracticalTourViewModel(PracticalTourStateMachine stateMachine) : ObservableObject
{
    private bool _tutorialSampleQueued;
    private IReadOnlyList<PracticalTourStepRow> _steps = [];

    public PracticalTourStep CurrentStep => stateMachine.CurrentStep;
    public bool HasCleanRun => stateMachine.HasCleanRun;
    public bool HasSingleVisualization => stateMachine.HasSingleVisualization;
    public string? FirstExportFormat => stateMachine.FirstExportFormat;
    public IReadOnlyList<PracticalTourStepRow> Steps => _steps;

    public string Instruction => GetInstruction(CurrentStep);

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
        _steps = PracticalTourStepCatalog.VisibleSteps.Select(CreateStepRow).ToArray();
        OnPropertyChanged(nameof(Steps));
    }

    private PracticalTourStepRow CreateStepRow(PracticalTourStepDefinition definition)
    {
        bool isUnavailable = IsUnavailable(definition);
        int currentIndex = IndexOfVisibleStep(CurrentStep);
        int stepIndex = IndexOfVisibleStep(definition.Step);
        return new(
            GetTitle(definition),
            GetInstruction(definition.Step),
            definition.Step == CurrentStep,
            !isUnavailable && (CurrentStep == PracticalTourStep.Completed || stepIndex < currentIndex),
            isUnavailable);
    }

    private static int IndexOfVisibleStep(PracticalTourStep step) =>
        Array.FindIndex(PracticalTourStepCatalog.VisibleSteps, definition => definition.Step == step);

    private bool IsUnavailable(PracticalTourStepDefinition definition) =>
        definition.NeedsFindings && stateMachine.HasCleanRun
        || definition.NeedsVisualizationChoice && stateMachine.HasSingleVisualization;

    private string GetTitle(PracticalTourStepDefinition definition) =>
        Localize(IsUnavailable(definition) ? definition.UnavailableTitleKey ?? definition.TitleKey : definition.TitleKey);

    private string GetInstruction(PracticalTourStep step)
    {
        PracticalTourStepDefinition definition = PracticalTourStepCatalog.Get(step);
        if (step == PracticalTourStep.OpenDocument && _tutorialSampleQueued)
            return Localize(definition.TutorialSampleInstructionKey);
        if (step == PracticalTourStep.ExportReport && stateMachine.HasCleanRun)
            return Localize(definition.CleanRunInstructionKey);
        return Localize(definition.InstructionKey);
    }

    private static string Localize(string? resourceKey) =>
        resourceKey is null
            ? string.Empty
            : LocalizationResourceReader.GetString(PracticalTourStepCatalog.ResourceBaseName, resourceKey);

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
