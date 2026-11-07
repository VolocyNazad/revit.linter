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

            configurationInstaller.Install();

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

    private void ViewModel_StopRequested(object? sender, EventArgs args)
    {
        bool isCompleted = _viewModel?.CurrentStep == PracticalTourStep.Completed;
        EndSession(hidePane: true);
        if (!isCompleted) return;
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
            ClearCatalogScope();
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

    private void ClearCatalogScope()
    {
        try
        {
            catalogScope.Clear();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to restore the full diagnostic catalog after the practical tour");
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
            ClearCatalogScope();
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
