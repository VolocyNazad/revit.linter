using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
using Revit.Linter.Localization;
using Revit.Linter.OnboardingState.Models;
using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Infrastructure.Services;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.WelcomePresenter.ViewModels;

/// <summary>
/// Presents the welcome wizard: it decides which steps are pending, navigates between them and starts the
/// practical tour when the user finishes.
/// </summary>
[GenerateLocalizedProperties]
internal sealed partial class WelcomeViewModel(
    IValueStore<OnboardingSettings> settingsStore,
    IWelcomeHost host,
    IPracticalTour practicalTour,
    ILogger<WelcomeViewModel> logger,
    WelcomeIntroStepViewModel introStep,
    WelcomeExamplesStepViewModel examplesStep,
    WelcomePracticalStepViewModel practicalStep,
    WelcomeWindowFactory windowFactory,
    IConfigurationPaths paths) : ObservableObject, IWelcomeWizard
{
    /// <summary>
    /// The version of the wizard content. Increase it when a new step or new important information must be
    /// shown again to users who already completed an earlier version.
    /// </summary>
    internal const int CurrentWizardVersion = 5;

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

    /// <summary>Gets a value indicating whether the wizard shows a step other than the main page.</summary>
    public bool CanGoBack => _currentIndex > 0;

    /// <summary>
    /// Gets the command of the window footer action: it writes the examples on their step and closes the
    /// window with the tour request on the practical step.
    /// </summary>
    public ICommand? PrimaryActionCommand => CurrentStep switch
    {
        WelcomeExamplesStepViewModel examples => examples.InstallCommand,
        _ when CurrentStep?.HasPrimaryAction == true => CloseWithResultCommand,
        _ => null,
    };

    /// <summary>Gets the installed product version shown next to the title.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarAnalyzer", "S2325",
        Justification = "XAML binds to instance members only.")]
    public string VersionText => GetLocalizedString("version_format", ProductVersion);

    private static string ProductVersion
    {
        get
        {
            Assembly assembly = typeof(WelcomeViewModel).Assembly;
            string? informationalVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            string version = informationalVersion ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(version))
            {
                // GitVersion appends the commit height and hash after '+': keep the numeric product version.
                int metadataSeparator = version.IndexOf('+');
                return metadataSeparator < 0
                    ? version
                    : version.Substring(0, metadataSeparator);
            }

            return assembly.GetName().Version?.ToString(3) ?? string.Empty;
        }
    }

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
            // The practical tour follows the main page; the examples step closes the wizard.
            Steps.Add(introStep);
            Steps.Add(practicalStep);
            if (plan.IncludeExamples) Steps.Add(examplesStep);
        }
        else
        {
            // A pending examples offer stays the first step of the wizard.
            if (plan.IncludeExamples) Steps.Add(examplesStep);
            Steps.Add(practicalStep);
        }
        if (plan.IncludeIntro) introStep.SetContents(Steps.Skip(1), GoToStepCommand);
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

    private bool CanGoBackStep() => CanGoBack;

    [RelayCommand(CanExecute = nameof(CanGoBackStep))]
    private void GoBack()
    {
        // The main page is the home of every step, so Back returns to it instead of the previous step.
        int homeIndex = Steps.IndexOf(introStep);
        MoveTo(homeIndex >= 0 ? homeIndex : _currentIndex - 1);
    }

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
        CurrentStep = Steps[index];

        if (ReferenceEquals(CurrentStep, practicalStep))
        {
            List<ExampleDiscipline> disciplines = [];
            if (examplesStep.IsMepSelected) disciplines.Add(ExampleDiscipline.Mep);
            if (examplesStep.IsArchitectureSelected) disciplines.Add(ExampleDiscipline.Architecture);
            if (examplesStep.IsStructureSelected) disciplines.Add(ExampleDiscipline.Structure);
            practicalStep.RefreshTutorialSamples(disciplines);
        }

        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(PrimaryActionCommand));
        GoBackCommand.NotifyCanExecuteChanged();
    }
}
