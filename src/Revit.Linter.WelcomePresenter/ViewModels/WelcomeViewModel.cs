using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
using Revit.Linter.Localization;
using Revit.Linter.WelcomePresenter.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Infrastructure.Services;
using System.Collections.ObjectModel;
using System.Windows;
using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.WelcomePresenter.ViewModels;

/// <summary>
/// Presents the welcome wizard: it decides which steps are pending, navigates between them and starts the
/// practical tour when the user finishes.
/// </summary>
[GenerateLocalizedProperties]
internal sealed partial class WelcomeViewModel(
    IValueStore<WelcomeSettings> settingsStore,
    IWelcomeHost host,
    IPracticalTour practicalTour,
    ILogger<WelcomeViewModel> logger,
    WelcomeIntroStepViewModel introStep,
    WelcomeRulesStepViewModel rulesStep,
    WelcomeExamplesStepViewModel examplesStep,
    WelcomePracticalStepViewModel practicalStep,
    WelcomeFinishStepViewModel finishStep,
    WelcomeWindowFactory windowFactory,
    IConfigurationPaths paths) : ObservableObject, IWelcomeWizard
{
    /// <summary>
    /// The version of the wizard content. Increase it when a new step or new important information must be
    /// shown again to users who already completed an earlier version.
    /// </summary>
    internal const int CurrentWizardVersion = 3;

    private int _currentIndex;

    public ObservableCollection<WelcomeStepViewModel> Steps { get; } = [];

    [ObservableProperty]
    public partial WelcomeStepViewModel? CurrentStep { get; private set; }

    /// <summary>
    /// Gets the dialog result requested by the finish command; the window behavior applies it.
    /// </summary>
    [ObservableProperty]
    public partial bool? DialogResult { get; private set; }

    /// <summary>Closes the wizard with the given result.</summary>
    [RelayCommand]
    private void CloseWithResult(bool result) => DialogResult = result;

    public bool IsLastStep => _currentIndex == Steps.Count - 1;

    public bool HasNextStep => _currentIndex < Steps.Count - 1;

    public string Progress => GetLocalizedString("step_format", _currentIndex + 1, Steps.Count);

    public bool ShowIfNeeded()
    {
        WelcomeWizardPlan plan = WelcomeWizardState.GetPendingPlan(
            settingsStore.CurrentValue, CurrentWizardVersion, paths.RevitVersion);
        if (!plan.HasSteps) return false;

        ShowSteps(plan);
        return true;
    }

    public void Show() => ShowSteps(WelcomeWizardPlan.AllSteps);

    private void ShowSteps(WelcomeWizardPlan plan)
    {
        Steps.Clear();
        if (plan.IncludeIntro)
        {
            Steps.Add(introStep);
            Steps.Add(rulesStep);
        }
        if (plan.IncludeExamples) Steps.Add(examplesStep);
        Steps.Add(practicalStep);
        Steps.Add(finishStep);
        for (int index = 0; index < Steps.Count; index++)
            Steps[index].Number = index + 1;
        MoveTo(0);

        Window window = windowFactory.Create(this);
        DialogResult = null;

        bool finished = window.ShowDialog() == true;
        DialogResult = null;

        // Closing the window in any way counts as shown; the ribbon command reopens the wizard.
        settingsStore.Update(settings => WelcomeWizardState.MarkShown(
            settings, plan, CurrentWizardVersion, paths.RevitVersion));
        logger.LogInformation(
            "Welcome wizard closed (finished: {Finished}, examples installed: {ExamplesInstalled})",
            finished, examplesStep.IsInstalled);

        if (finished)
        {
            bool sampleQueued = practicalStep.StartPracticalTour
                && practicalStep.PrepareTutorialSample(paths.RevitVersion);
            if (practicalStep.StartPracticalTour && host.HasOpenDocument) host.ShowPanes();
            if (sampleQueued)
                practicalTour.Start(hasOpenDocument: false, restart: true, tutorialSampleQueued: true);
            else if (practicalStep.StartPracticalTour)
                practicalTour.Start(host.HasOpenDocument);
            else
                // The user refused the tour: no session starts, so a restored pane would stay
                // visible with no content. Hide it; the ribbon command resumes the tour later.
                practicalTour.HidePaneIfInactive();
        }
        else
        {
            // The wizard was cancelled without starting the tour: same as a refusal, there is
            // no session to fill a restored pane.
            practicalTour.HidePaneIfInactive();
        }
    }

    private bool CanGoBack() => _currentIndex > 0;

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack() => MoveTo(_currentIndex - 1);

    private bool CanGoNext() => HasNextStep;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void GoNext() => MoveTo(_currentIndex + 1);

    [RelayCommand]
    private void GoToStep(WelcomeStepViewModel? step)
    {
        if (step is null) return;

        int index = Steps.IndexOf(step);
        if (index >= 0 && index != _currentIndex) MoveTo(index);
    }

    private void MoveTo(int index)
    {
        _currentIndex = index;
        for (int stepIndex = 0; stepIndex < Steps.Count; stepIndex++)
            Steps[stepIndex].IsCurrent = stepIndex == index;
        CurrentStep = Steps[index];

        if (ReferenceEquals(CurrentStep, practicalStep))
        {
            List<ExampleDiscipline> disciplines = [];
            if (examplesStep.IsMepSelected) disciplines.Add(ExampleDiscipline.Mep);
            if (examplesStep.IsArchitectureSelected) disciplines.Add(ExampleDiscipline.Architecture);
            if (examplesStep.IsStructureSelected) disciplines.Add(ExampleDiscipline.Structure);
            practicalStep.RefreshTutorialSamples(disciplines);
        }

        OnPropertyChanged(nameof(IsLastStep));
        OnPropertyChanged(nameof(HasNextStep));
        OnPropertyChanged(nameof(Progress));
        GoBackCommand.NotifyCanExecuteChanged();
        GoNextCommand.NotifyCanExecuteChanged();
    }
}
