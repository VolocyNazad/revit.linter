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
            if (_viewModel is not null) EndSession(hidePane: false);
            int revitVersion = ConfigurationPathUtils.RevitVersion;
            PracticalTourProgress? savedProgress = GetProgress(revitVersion);
            if (restart || savedProgress is { IsCompleted: true } or { IsOptedOut: true })
            {
                // Every Start call carries an explicit user intent to tour again, so a
                // finished or refused tour starts over instead of hiding the pane: the
                // saved state is forgotten and the managed configuration is reinstalled
                // below. Pane visibility without a session is handled by HidePaneIfInactive.
                if (!restart)
                    logger.LogInformation(
                        "Restarting the practical tour after its saved {SavedState} state for Revit {RevitVersion}",
                        savedProgress is { IsCompleted: true } ? "completed" : "opted-out",
                        revitVersion);
                ResetProgress();
                savedProgress = null;
            }

            exampleConfigurationInstaller.InstallPracticalTour();

            if (savedProgress is null)
            {
                // A fresh tour starts with the managed check switched off because the installed
                // tour configuration declares isActive: false; the user enables it as the guided
                // step asks. Resumed tours keep the selection written to the managed file.
            }

            PracticalTourViewModel viewModel = new(new PracticalTourStateMachine());
            view.DataContext = viewModel;
            viewModel.StopRequested += ViewModel_StopRequested;
            viewModel.OptOutRequested += ViewModel_OptOutRequested;
            viewModel.RestartRequested += ViewModel_RestartRequested;
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
                // A run counts only with the single training check selected: mixed runs
                // would credit findings from unrelated rules. (A list pattern would need
                // System.Index, which the net48 WPF markup compilation lacks.)
                if (run.Result != DiagnosticServiceResult.Failed
                    && run.EnabledDiagnosticCodes.Length == 1
                    && string.Equals(run.EnabledDiagnosticCodes[0], DiagnosticCode, StringComparison.Ordinal))
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

            if (activity is DiagnosticSearchChangedActivity search)
            {
                _viewModel?.ObserveSearchText(search.SearchText);
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

            if (activity is IgnoreAppliedActivity ignored)
            {
                if (IsPracticalTourDiagnostic(ignored.DiagnosticCode))
                    _viewModel?.Observe(PracticalTourStep.UnderstandIgnore);
                return;
            }

            if (activity is IgnoreListPaneShownActivity)
            {
                _viewModel?.Observe(PracticalTourStep.IgnoreList);
                return;
            }

            if (activity is IgnoreSelectedActivity selectedEntry)
            {
                if (IsPracticalTourDiagnostic(selectedEntry.DiagnosticCode))
                    _viewModel?.Observe(PracticalTourStep.IgnoreList);
                return;
            }

            if (activity is DocumentationOpenedActivity)
            {
                _viewModel?.Observe(PracticalTourStep.OpenHelp);
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

            if (activity is DocumentFocusChangedActivity)
            {
                if (EndOnDocumentChange())
                {
                    logger.LogInformation(
                        "Practical tour ended on document change at step {Step}", _viewModel?.CurrentStep);
                    EndSession(hidePane: true);
                }
                return;
            }

            PracticalTourStep step = activity switch
            {
                DocumentOpenedActivity => PracticalTourStep.OpenDocument,
                _ => PracticalTourStep.Completed,
            };
            if (activity is DocumentOpenedActivity)
            {
                welcomeHost.ShowPanes();
                if (EndOnDocumentChange())
                {
                    logger.LogInformation(
                        "Practical tour ended on document change at step {Step}", _viewModel?.CurrentStep);
                    EndSession(hidePane: true);
                    return;
                }
            }
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

    // Findings, visualizations and fixes observed so far belong to the previous document, and the
    // shared TOUR001 check exists in every document, so continuing would advance the tour on
    // unrelated findings. Only waiting for a document survives a document change: opening one
    // is the awaited action itself.
    private bool EndOnDocumentChange()
    {
        if (_viewModel is null) return false;
        return _viewModel.CurrentStep != PracticalTourStep.OpenDocument;
    }

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
            PracticalTourStep.OpenHelp => "OpenHelp",
            PracticalTourStep.ShowElement => "ShowElement",
            PracticalTourStep.SelectVisualization => "ShowElement",
        PracticalTourStep.NavigateFindings => "NavigateFindings",
        PracticalTourStep.UnderstandFix => "UnderstandFix",
        PracticalTourStep.UnderstandIgnore => "UnderstandFix",
        PracticalTourStep.IgnoreList => "IgnoreList",
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

    /// <summary>Brings forward the pane where the current step happens.</summary>
    /// <remarks>
    /// Step-to-pane map: opening, configuration, search, selection and run steps show Diagnostics;
    /// finding, help, visualization, navigation and fix-or-ignore steps show Warnings; FixList shows
    /// the fix list; IgnoreList shows the ignore list. Export steps keep the current pane so the user
    /// is not yanked away right after selecting a fix row, and completion hides the tour pane itself.
    /// </remarks>
    private void ShowTargetPane(PracticalTourStep step)
    {
        // The tour starts on the diagnostics list and returns to it for the early steps:
        // opening steps carry no target of their own, so they keep the starting pane.
        if (step is PracticalTourStep.OpenDocument
            or PracticalTourStep.OpenConfigurationFolder
            or PracticalTourStep.SearchAndFilters
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

        if (step is PracticalTourStep.IgnoreList)
        {
            welcomeHost.ShowPane(WelcomePane.IgnoreList);
            return;
        }

        // ExportReport keeps the current pane: yanking the user away right after
        // they selected a fix row hides what they came to look at.
        if (step is PracticalTourStep.InspectFinding
            or PracticalTourStep.OpenHelp
            or PracticalTourStep.ShowElement
            or PracticalTourStep.SelectVisualization
            or PracticalTourStep.NavigateFindings
            or PracticalTourStep.UnderstandFix
            or PracticalTourStep.UnderstandIgnore)
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

    public void HidePaneIfInactive()
    {
        if (_viewModel is not null) return;

        try
        {
            welcomeHost.HidePracticalTourPane();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to hide the inactive practical-tour pane");
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
