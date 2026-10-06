using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Behaviors;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.ThemeManaging.Abstractions.Services;
using Revit.Linter.WelcomePresenter.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Abstractions.Services;
using Revit.Linter.WelcomePresenter.ViewModels;
using Revit.Linter.WelcomePresenter.Views;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.WelcomePresenter.Infrastructure.Services;

internal sealed class PracticalTour(
    IThemeService themeService,
    IUserInterfaceActivityStream activityStream,
    IValueStore<WelcomeSettings> settingsStore,
    IValueStore<ElementDiagnosticOverridesSettings> elementOverrideStore,
    IWelcomeHost welcomeHost,
    IExampleConfigurationInstaller exampleConfigurationInstaller,
    PracticalTourView view,
    ILogger<PracticalTour> logger) : IPracticalTour
{
    internal const string DiagnosticCode = "TOUR001";

    private PracticalTourViewModel? _viewModel;
    private OnboardingHighlightSession? _highlightSession;
    private bool _optOutRequested;

    public void Start(bool hasOpenDocument, bool restart = false, bool tutorialSampleQueued = false)
    {
        try
        {
            exampleConfigurationInstaller.InstallPracticalTour();
            if (_viewModel is not null) EndSession(hidePane: false);
            int revitVersion = ConfigurationPathUtils.RevitVersion;
            PracticalTourProgress? savedProgress = GetProgress(revitVersion);
            if (restart)
            {
                ResetProgress();
                savedProgress = null;
            }
            else if (savedProgress is { IsCompleted: true } or { IsOptedOut: true })
            {
                return;
            }

            if (savedProgress is null)
            {
                // A fresh tour starts with the managed check switched off so the user
                // enables it as the guided step asks. Clearing the persisted override
                // falls back to the configuration default and publishes no activity,
                // so the tour cannot advance spuriously. Resumed tours keep the selection.
                elementOverrideStore.Update(settings => settings.Overrides.Remove(DiagnosticCode));
            }

            PracticalTourViewModel viewModel = new(new PracticalTourStateMachine());
            view.DataContext = viewModel;
            viewModel.StopRequested += ViewModel_StopRequested;
            viewModel.OptOutRequested += ViewModel_OptOutRequested;
            viewModel.RestartRequested += ViewModel_RestartRequested;
            view.PaneHidden += View_PaneHidden;
            _viewModel = viewModel;
            _highlightSession = OnboardingHighlight.StartSession();
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            activityStream.ActivityPublished += ActivityStream_ActivityPublished;
            themeService.Register(view);
            _optOutRequested = false;
            viewModel.Start(hasOpenDocument, savedProgress, tutorialSampleQueued);
            SaveProgress(viewModel.CurrentStep, viewModel.CurrentStep == PracticalTourStep.Completed, isOptedOut: false);
            UpdateGuidance(viewModel.CurrentStep);
            welcomeHost.ShowPracticalTourPane();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Practical tour was disabled after an onboarding failure");
            CloseAfterFailure();
        }
    }

    private void ActivityStream_ActivityPublished(object? sender, UserInterfaceActivity activity)
    {
        try
        {
            if (activity is DiagnosticRunCompletedActivity run)
            {
                if (run.Result != DiagnosticServiceResult.Failed)
                    _viewModel?.ObserveDiagnosticRun(run.FindingCount > 0);
                return;
            }

            if (activity is VisualizationAppliedActivity visualization)
            {
                if (!IsPracticalTourDiagnostic(visualization.DiagnosticCode)) return;
                _viewModel?.ObserveVisualization(
                    visualization.SelectedFromMenu,
                    visualization.AvailableOptionCount);
                return;
            }

            if (activity is DiagnosticListFilteredActivity)
            {
                _viewModel?.Observe(PracticalTourStep.SearchAndFilters);
                return;
            }

            if (activity is FixListPaneShownActivity)
            {
                _viewModel?.Observe(PracticalTourStep.FixList);
                return;
            }

            if (activity is FixSelectedActivity selectedFix)
            {
                if (IsPracticalTourDiagnostic(selectedFix.DiagnosticCode))
                    _viewModel?.Observe(PracticalTourStep.FixList);
                return;
            }

            if (activity is DiagnosticSelectionChangedActivity selection)
            {
                if (selection.IsActive && IsPracticalTourDiagnostic(selection.DiagnosticCode))
                    _viewModel?.Observe(PracticalTourStep.SelectDiagnostic);
                return;
            }

            if (activity is FindingSelectedActivity finding)
            {
                if (IsPracticalTourDiagnostic(finding.DiagnosticCode))
                    _viewModel?.Observe(PracticalTourStep.InspectFinding);
                return;
            }

            if (activity is FixAppliedActivity applied)
            {
                if (IsPracticalTourDiagnostic(applied.DiagnosticCode))
                    _viewModel?.Observe(PracticalTourStep.UnderstandFix);
                return;
            }

            if (activity is FindingNavigationUsedActivity navigation)
            {
                if (IsPracticalTourDiagnostic(navigation.DiagnosticCode))
                    _viewModel?.Observe(PracticalTourStep.NavigateFindings);
                return;
            }

            if (activity is RuleConfigurationOpenedActivity ruleConfiguration
                && IsPracticalTourDiagnostic(ruleConfiguration.DiagnosticCode))
            {
                _viewModel?.Observe(PracticalTourStep.OpenConfigurationFolder);
                return;
            }

            if (activity is DiagnosticReportExportedActivity exported)
            {
                _viewModel?.ObserveReportExport(exported.Format);
                return;
            }

            PracticalTourStep step = activity switch
            {
                DocumentOpenedActivity => PracticalTourStep.OpenDocument,
                _ => PracticalTourStep.Completed,
            };
            if (activity is DocumentOpenedActivity) welcomeHost.ShowPanes();
            _viewModel?.Observe(step);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Practical tour was disabled while observing user activity");
            CloseAfterFailure();
        }
    }

    private static bool IsPracticalTourDiagnostic(string diagnosticCode)
        => string.Equals(diagnosticCode, DiagnosticCode, StringComparison.Ordinal);

    private void ViewModel_StopRequested(object? sender, EventArgs args)
    {
        bool isCompleted = _viewModel?.CurrentStep == PracticalTourStep.Completed;
        EndSession(hidePane: true);
        if (!isCompleted) return;
        RemovePracticalTourConfiguration();
        ResetDiagnosticsSearchAndFilters();
        try
        {
            welcomeHost.ShowPracticalTourCompletedNotification();
            logger.LogInformation(
                "Practical tour completed for Revit {RevitVersion}", ConfigurationPathUtils.RevitVersion);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to show the practical-tour completion notification");
        }
    }

    private void ViewModel_OptOutRequested(object? sender, EventArgs args)
    {
        _optOutRequested = true;
        EndSession(hidePane: true);
        RemovePracticalTourConfiguration();
        ResetDiagnosticsSearchAndFilters();
    }

    private void ViewModel_RestartRequested(object? sender, EventArgs args)
    {
        // Start discards the current session and saved progress when restarted,
        // reinstalls the managed configuration and resumes at the first applicable step.
        Start(welcomeHost.HasOpenDocument, restart: true);
    }

    private void RemovePracticalTourConfiguration()
    {
        try
        {
            exampleConfigurationInstaller.RemovePracticalTour();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to remove the practical-tour diagnostic configuration");
        }
    }

    private void ResetDiagnosticsSearchAndFilters()
    {
        try
        {
            welcomeHost.ResetDiagnosticsSearchAndFilters();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to reset the diagnostics search and filters");
        }
    }

    private void View_PaneHidden(object? sender, EventArgs args)
        => EndSession(hidePane: false);

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(PracticalTourViewModel.CurrentStep)
            || sender is not PracticalTourViewModel viewModel) return;

        try
        {
            SaveProgress(
                viewModel.CurrentStep,
                viewModel.CurrentStep == PracticalTourStep.Completed,
                isOptedOut: false);
            UpdateGuidance(viewModel.CurrentStep);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Practical tour was disabled while updating its progress");
            CloseAfterFailure();
        }
    }

    private void UpdateHighlight(PracticalTourStep step) => _highlightSession?.SetActiveKey(step switch
    {
        PracticalTourStep.OpenConfigurationFolder => "OpenConfiguration",
        PracticalTourStep.SearchAndFilters => "SearchAndFilters",
        PracticalTourStep.SelectDiagnostic => "SelectDiagnostic",
            PracticalTourStep.RunDiagnostics => "RunDiagnostics",
            PracticalTourStep.InspectFinding => "InspectFinding",
            PracticalTourStep.ShowElement => "ShowElement",
            PracticalTourStep.SelectVisualization => "ShowElement",
        PracticalTourStep.NavigateFindings => "NavigateFindings",
        PracticalTourStep.UnderstandFix => "UnderstandFix",
        PracticalTourStep.FixList => "FixList",
            PracticalTourStep.ExportReport => "ExportReport",
            PracticalTourStep.ExportReportAnotherFormat => "ExportReport",
            _ => null,
        });

    private void UpdateGuidance(PracticalTourStep step)
    {
        ShowTargetPane(step);
        UpdateHighlight(step);
    }

    private void ShowTargetPane(PracticalTourStep step)
    {
        if (step is PracticalTourStep.SearchAndFilters
            or PracticalTourStep.SelectDiagnostic
            or PracticalTourStep.RunDiagnostics)
        {
            welcomeHost.ShowPane(WelcomePane.Diagnostics);
            return;
        }

        if (step is PracticalTourStep.FixList)
        {
            welcomeHost.ShowPane(WelcomePane.FixList);
            return;
        }

        // ExportReport keeps the current pane: yanking the user away right after
        // they selected a fix row hides what they came to look at.
        if (step is PracticalTourStep.InspectFinding
            or PracticalTourStep.ShowElement
            or PracticalTourStep.SelectVisualization
            or PracticalTourStep.NavigateFindings
            or PracticalTourStep.UnderstandFix)
            welcomeHost.ShowPane(WelcomePane.Warnings);
    }

    public void ResetProgress()
    {
        try
        {
            int revitVersion = ConfigurationPathUtils.RevitVersion;
            settingsStore.Update(settings => settings.PracticalTourProgressByRevitVersion.Remove(revitVersion));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to reset practical-tour progress");
        }
    }

    private void CloseAfterFailure()
    {
        try
        {
            EndSession(hidePane: true, saveProgress: false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to close the practical-tour pane after an onboarding failure");
        }
    }

    private void EndSession(bool hidePane, bool saveProgress = true)
    {
        PracticalTourViewModel? viewModel = _viewModel;
        if (viewModel is null) return;

        try
        {
            if (saveProgress)
            {
                SaveProgress(
                    viewModel.CurrentStep,
                    viewModel.CurrentStep == PracticalTourStep.Completed && !_optOutRequested,
                    _optOutRequested);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to save practical-tour progress while closing");
        }
        finally
        {
            activityStream.ActivityPublished -= ActivityStream_ActivityPublished;
            viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            viewModel.StopRequested -= ViewModel_StopRequested;
            viewModel.OptOutRequested -= ViewModel_OptOutRequested;
            viewModel.RestartRequested -= ViewModel_RestartRequested;
            view.PaneHidden -= View_PaneHidden;
            _highlightSession?.Dispose();
            _highlightSession = null;
            _viewModel = null;
            view.DataContext = null;
            if (hidePane) welcomeHost.HidePracticalTourPane();
        }
    }

    private PracticalTourProgress? GetProgress(int revitVersion)
        => settingsStore.CurrentValue.PracticalTourProgressByRevitVersion.TryGetValue(
            revitVersion, out PracticalTourProgress? progress)
            ? progress
            : null;

    private void SaveProgress(PracticalTourStep step, bool isCompleted, bool isOptedOut)
    {
        int revitVersion = ConfigurationPathUtils.RevitVersion;
        bool hasCleanRun = _viewModel?.HasCleanRun == true;
        bool hasSingleVisualization = _viewModel?.HasSingleVisualization == true;
        string? firstExportFormat = _viewModel?.FirstExportFormat;
        settingsStore.Update(settings => settings.PracticalTourProgressByRevitVersion[revitVersion] = new()
        {
            CurrentStep = step,
            HasCleanRun = hasCleanRun,
            HasSingleVisualization = hasSingleVisualization,
            FirstExportFormat = firstExportFormat,
            IsCompleted = isCompleted,
            IsOptedOut = isOptedOut,
        });
    }
}
