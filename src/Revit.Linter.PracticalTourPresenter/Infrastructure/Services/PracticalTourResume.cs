using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions.Models;

namespace Revit.Linter.PracticalTourPresenter.Infrastructure.Services;

/// <summary>Chooses the step a saved practical-tour session resumes from.</summary>
/// <remarks>
/// A resumed session can continue at a finding-specific step only when the findings it explains are still
/// displayed for the active document. That fact is checked instead of persisting a document identity: the
/// tour often runs on a disposable sample copy whose path changes between runs, and the same rule then also
/// covers a restarted Revit or a report cleared by another diagnostic run.
/// </remarks>
internal static class PracticalTourResume
{
    /// <summary>Returns the progress the session starts from.</summary>
    /// <param name="progress">The saved progress.</param>
    /// <param name="hasOpenDocument">Whether a document is open in Revit.</param>
    /// <param name="hasTrainingFindings">Whether the active document shows findings of the training check.</param>
    /// <returns>
    /// The saved progress, or a copy with the step the session can actually continue from.
    /// </returns>
    internal static PracticalTourProgress ResolveStartProgress(
        PracticalTourProgress progress,
        bool hasOpenDocument,
        bool hasTrainingFindings)
    {
        PracticalTourStep step = ResolveStartStep(
            progress.CurrentStep,
            hasOpenDocument,
            progress.HasCleanRun,
            hasTrainingFindings);
        return step == progress.CurrentStep ? progress : new PracticalTourProgress
        {
            CurrentStep = step,
            HasCleanRun = progress.HasCleanRun,
            HasSingleVisualization = progress.HasSingleVisualization,
            FirstExportFormat = progress.FirstExportFormat,
            IsCompleted = progress.IsCompleted,
            IsOptedOut = progress.IsOptedOut,
        };
    }

    /// <summary>Returns the step the session can continue from.</summary>
    /// <param name="savedStep">The saved next step.</param>
    /// <param name="hasOpenDocument">Whether a document is open in Revit.</param>
    /// <param name="hasCleanRun">Whether the saved run found no findings.</param>
    /// <param name="hasTrainingFindings">Whether the active document shows findings of the training check.</param>
    /// <remarks>
    /// Without a document the session waits at the open-document step. A finding-specific step needs the
    /// findings it explains; a clean run has none by design, so it keeps its saved step. Any other step is
    /// document-independent and is kept as saved.
    /// </remarks>
    internal static PracticalTourStep ResolveStartStep(
        PracticalTourStep savedStep,
        bool hasOpenDocument,
        bool hasCleanRun,
        bool hasTrainingFindings)
    {
        if (!hasOpenDocument) return PracticalTourStep.OpenDocument;
        if (savedStep == PracticalTourStep.Completed) return savedStep;
        if (hasCleanRun || !IsAfterRunStep(savedStep)) return savedStep;

        return hasTrainingFindings ? savedStep : PracticalTourStep.RunDiagnostics;
    }

    private static bool IsAfterRunStep(PracticalTourStep step)
    {
        int stepIndex = Array.IndexOf(PracticalTourStepCatalog.Steps, step);
        int runIndex = Array.IndexOf(PracticalTourStepCatalog.Steps, PracticalTourStep.RunDiagnostics);
        return stepIndex > runIndex;
    }
}
