using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Behaviors;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
using Revit.Linter.ThemeManaging.Abstractions.Services;
using Revit.Linter.OnboardingState.Models;
using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions;
using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions.Models;
using Revit.Linter.PracticalTourPresenter.ViewModels;
using Revit.Linter.PracticalTourPresenter.Views;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.PracticalTourPresenter.Infrastructure.Services;

/// <summary>
/// Controls the modeless practical-tour session: installs the managed training configuration, advances the
/// tour from observed UI facts and persists the progress per Revit version.
/// </summary>
/// <remarks>A failure closes the session and leaves the add-in, the document and the other panes unaffected.</remarks>
internal sealed partial class PracticalTour(
    IThemeService themeService,
    IUserInterfaceActivityStream activityStream,
    IValueStore<OnboardingSettings> settingsStore,
    IPracticalTourHost tourHost,
    PracticalTourConfigurationInstaller configurationInstaller,
    PracticalTourView view,
    IConfigurationPaths paths,
    IDiagnosticCatalogScope catalogScope,
    ILogger<PracticalTour> logger) : IPracticalTour
{
    internal const string DiagnosticCode = "TOUR001";

    private PracticalTourViewModel? _viewModel;
    private OnboardingHighlightSession? _highlightSession;
    private IDisposable? _columnReveal;
    private bool _optOutRequested;

    public void Start(bool hasOpenDocument, bool restart = false, bool tutorialSampleQueued = false)
    {
        try
        {
            if (_viewModel is not null) EndSession(hidePane: false);
            int revitVersion = paths.RevitVersion;
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

            bool startsOver = savedProgress is null;
            configurationInstaller.Install(startsOver);
            catalogScope.BeginSession();

            // A session starts with a clean report: leftovers from a previous attempt, a document change or
            // an interrupted pause must not look like the findings the resumed step explains.
            ClearTrainingFindings();

            PracticalTourProgress? startProgress = savedProgress is null
                ? null
                : ResolveStartProgress(savedProgress, hasOpenDocument);
            bool resumedAtCheckpoint = savedProgress is not null
                                       && startProgress!.CurrentStep == PracticalTourStep.RunDiagnostics
                                       && savedProgress.CurrentStep != PracticalTourStep.RunDiagnostics;

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
            viewModel.Start(hasOpenDocument, startProgress, tutorialSampleQueued, resumedAtCheckpoint);

            // The guided steps point at the training check row, so a search, filter or column sort left
            // over from earlier work must not hide it.
            tourHost.ResetDiagnosticsSearchAndFilters();
            SaveProgress(viewModel.CurrentStep, viewModel.CurrentStep == PracticalTourStep.Completed, isOptedOut: false);
            UpdateCatalogScope(viewModel.CurrentStep);
            UpdateGuidance(viewModel.CurrentStep);
            tourHost.ShowPracticalTourPane();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Practical tour was disabled after an onboarding failure");
            CloseAfterFailure();
        }
    }

    /// <summary>Chooses the step a saved session can continue from.</summary>
    /// <param name="savedProgress">The saved progress.</param>
    /// <param name="hasOpenDocument">Whether a document is open in Revit.</param>
    /// <remarks>
    /// The run of the training check is the checkpoint: a saved step after it continues from the run
    /// again, because the findings that step explained are not kept across a pause.
    /// </remarks>
    private PracticalTourProgress ResolveStartProgress(PracticalTourProgress savedProgress, bool hasOpenDocument)
    {
        PracticalTourProgress progress = PracticalTourResume.ResolveStartProgress(savedProgress, hasOpenDocument);
        if (progress.CurrentStep != savedProgress.CurrentStep)
            logger.LogInformation(
                "The practical tour continues from its run checkpoint instead of the saved {SavedStep}",
                savedProgress.CurrentStep);
        return progress;
    }

    private void ViewModel_StopRequested(object? sender, EventArgs args)
    {
        bool isCompleted = _viewModel?.CurrentStep == PracticalTourStep.Completed;
        EndSession(hidePane: true);
        if (!isCompleted)
        {
            // The paused session does not keep its findings: it resumes at the run checkpoint and finds
            // them again, so the user returns to a report that holds only their own results.
            ClearTrainingFindings();
            return;
        }
        RemovePracticalTourConfiguration();
        ResetDiagnosticsSearchAndFilters();
        try
        {
            tourHost.ShowPracticalTourCompletedNotification();
            logger.LogInformation(
                "Practical tour completed for Revit {RevitVersion}", paths.RevitVersion);
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
        Start(tourHost.HasOpenDocument, restart: true);
    }

    private void RemovePracticalTourConfiguration()
    {
        try
        {
            configurationInstaller.Remove();
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
            tourHost.ResetDiagnosticsSearchAndFilters();
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
            UpdateCatalogScope(viewModel.CurrentStep);
            UpdateGuidance(viewModel.CurrentStep);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Practical tour was disabled while updating its progress");
            CloseAfterFailure();
        }
    }

    private void UpdateHighlight(PracticalTourStep step) =>
        _highlightSession?.SetActiveKey(PracticalTourStepCatalog.Get(step).HighlightKey);

    private void UpdateGuidance(PracticalTourStep step)
    {
        ShowTargetPane(step);
        UpdateHighlight(step);
        UpdateColumnReveal(step);
    }

    /// <summary>Shows the column a step points at for as long as the step lasts.</summary>
    /// <remarks>
    /// A hidden column would leave the spotlight over an empty header. The step reveals the column and
    /// restores the visibility the user chose when the tour moves on, pauses or ends; the reveal is
    /// session state, so it never writes the stored column choice.
    /// </remarks>
    private void UpdateColumnReveal(PracticalTourStep step)
    {
        ReleaseColumnReveal();
        if (PracticalTourStepCatalog.Get(step).Column is not { } column) return;

        try
        {
            _columnReveal = tourHost.RevealTableColumn(column);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to reveal the column of the practical-tour step");
        }
    }

    private void ReleaseColumnReveal()
    {
        _columnReveal?.Dispose();
        _columnReveal = null;
    }

    /// <summary>Publishes only the training check while the tour works with it.</summary>
    /// <remarks>
    /// The scope is session state, so it never writes the user's check states to the configuration and
    /// ending the session restores the full list. The catalog then republishes itself, and the report pane
    /// keeps its findings for that origin, so returning to a later step still has the report to work with.
    /// </remarks>
    private void UpdateCatalogScope(PracticalTourStep step)
    {
        if (PracticalTourStepCatalog.RequiresTrainingScope(step))
            FocusCatalogScope();
        else
            ClearCatalogScopeFocus();
    }

    private void FocusCatalogScope()
    {
        try
        {
            catalogScope.Focus(DiagnosticCode);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to narrow the diagnostic catalog to the training check");
        }
    }

    private void ClearCatalogScopeFocus()
    {
        try
        {
            catalogScope.Clear();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to publish every diagnostic of the practical tour");
        }
    }

    private void EndCatalogScope()
    {
        try
        {
            catalogScope.EndSession();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to hide the managed practical-tour checks after the session");
        }
    }

    private void ClearTrainingFindings()
    {
        try
        {
            tourHost.ClearDiagnosticFindings(DiagnosticCode);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to clear the practical-tour findings");
        }
    }

    /// <summary>Brings forward the pane where the current step happens.</summary>
    /// <remarks>
    /// The pane of every step is part of its catalog definition. A step without a pane keeps the current
    /// one, so export does not yank the user away right after they selected a fix row, and completion
    /// hides the tour pane itself.
    /// </remarks>
    private void ShowTargetPane(PracticalTourStep step)
    {
        if (PracticalTourStepCatalog.Get(step).Pane is { } pane)
            tourHost.ShowPane(pane);
    }

    public void ResetProgress()
    {
        try
        {
            int revitVersion = paths.RevitVersion;
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
            tourHost.HidePracticalTourPane();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to hide the inactive practical-tour pane");
        }
    }

    public void RefreshOutdatedConfiguration()
    {
        if (_viewModel is not null) return;

        try
        {
            configurationInstaller.RefreshOutdated();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to refresh the outdated practical-tour configuration");
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
        try
        {
            if (viewModel is not null && saveProgress)
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
            if (viewModel is not null)
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
            }

            ReleaseColumnReveal();
            EndCatalogScope();
            if (hidePane) tourHost.HidePracticalTourPane();
        }
    }

    private PracticalTourProgress? GetProgress(int revitVersion)
        => settingsStore.CurrentValue.PracticalTourProgressByRevitVersion.TryGetValue(
            revitVersion, out PracticalTourProgress? progress)
            ? progress
            : null;

    private void SaveProgress(PracticalTourStep step, bool isCompleted, bool isOptedOut)
    {
        int revitVersion = paths.RevitVersion;
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
