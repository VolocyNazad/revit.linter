using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Revit.Context.Abstractions.Services;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.Localization;
using Revit.Linter.ThemeManaging.Abstractions.Services;
using Revit.Linter.WelcomePresenter.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Infrastructure.Services;
using Revit.Linter.WelcomePresenter.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Interop;
using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.WelcomePresenter.ViewModels;

[GenerateLocalizedProperties]
internal sealed partial class WelcomeViewModel : ObservableObject, IWelcomeWizard
{
    /// <summary>
    /// The version of the wizard content. Increase it when a new step or new important information must be
    /// shown again to users who already completed an earlier version.
    /// </summary>
    internal const int CurrentWizardVersion = 1;

    private readonly IRevitContext _revitContext;
    private readonly IThemeService _themeService;
    private readonly IValueStore<WelcomeSettings> _settingsStore;
    private readonly IWelcomeHost _host;
    private readonly ILogger<WelcomeViewModel> _logger;
    private readonly WelcomeIntroStepViewModel _introStep;
    private readonly WelcomeExamplesStepViewModel _examplesStep;
    private readonly WelcomeFinishStepViewModel _finishStep;
    private int _currentIndex;

    public WelcomeViewModel(
        IRevitContext revitContext,
        IThemeService themeService,
        IValueStore<WelcomeSettings> settingsStore,
        IWelcomeHost host,
        ILogger<WelcomeViewModel> logger,
        WelcomeIntroStepViewModel introStep,
        WelcomeExamplesStepViewModel examplesStep,
        WelcomeFinishStepViewModel finishStep)
    {
        _revitContext = revitContext;
        _themeService = themeService;
        _settingsStore = settingsStore;
        _host = host;
        _logger = logger;
        _introStep = introStep;
        _examplesStep = examplesStep;
        _finishStep = finishStep;
    }

    public ObservableCollection<WelcomeStepViewModel> Steps { get; } = [];

    [ObservableProperty]
    public partial WelcomeStepViewModel? CurrentStep { get; private set; }

    public bool IsLastStep => _currentIndex == Steps.Count - 1;

    public bool HasNextStep => _currentIndex < Steps.Count - 1;

    public string Progress => GetLocalizedString("step_format", _currentIndex + 1, Steps.Count);

    public bool ShowIfNeeded()
    {
        WelcomeWizardPlan plan = WelcomeWizardState.GetPendingPlan(
            _settingsStore.CurrentValue, CurrentWizardVersion, ConfigurationPathUtils.RevitVersion);
        if (!plan.HasSteps) return false;

        ShowSteps(plan);
        return true;
    }

    public void Show() => ShowSteps(WelcomeWizardPlan.AllSteps);

    private void ShowSteps(WelcomeWizardPlan plan)
    {
        Steps.Clear();
        if (plan.IncludeIntro) Steps.Add(_introStep);
        if (plan.IncludeExamples) Steps.Add(_examplesStep);
        Steps.Add(_finishStep);
        for (int index = 0; index < Steps.Count; index++)
            Steps[index].Number = index + 1;
        MoveTo(0);

        WelcomeView window = new() { DataContext = this };
        _themeService.Register(window);

        IntPtr owner = _revitContext.UIApplication?.MainWindowHandle ?? IntPtr.Zero;
        if (owner == IntPtr.Zero)
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        else
            _ = new WindowInteropHelper(window) { Owner = owner };

        bool finished = window.ShowDialog() == true;

        // Closing the window in any way counts as shown; the ribbon command reopens the wizard.
        _settingsStore.Update(settings => WelcomeWizardState.MarkShown(
            settings, plan, CurrentWizardVersion, ConfigurationPathUtils.RevitVersion));
        _logger.LogInformation(
            "Welcome wizard closed (finished: {Finished}, examples installed: {ExamplesInstalled})",
            finished, _examplesStep.IsInstalled);

        if (finished && _finishStep.OpenPanes) _host.ShowPanes();
    }

    private bool CanGoBack() => _currentIndex > 0;

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack() => MoveTo(_currentIndex - 1);

    private bool CanGoNext() => HasNextStep;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void GoNext() => MoveTo(_currentIndex + 1);

    private void MoveTo(int index)
    {
        _currentIndex = index;
        for (int stepIndex = 0; stepIndex < Steps.Count; stepIndex++)
            Steps[stepIndex].IsCurrent = stepIndex == index;
        CurrentStep = Steps[index];

        if (IsLastStep && Steps.Contains(_examplesStep))
            _finishStep.Note = _examplesStep.IsInstalled ? null : _finishStep.ExamplesSkippedText;

        OnPropertyChanged(nameof(IsLastStep));
        OnPropertyChanged(nameof(HasNextStep));
        OnPropertyChanged(nameof(Progress));
        GoBackCommand.NotifyCanExecuteChanged();
        GoNextCommand.NotifyCanExecuteChanged();
    }
}
