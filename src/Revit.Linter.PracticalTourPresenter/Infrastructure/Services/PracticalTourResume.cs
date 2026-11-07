using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions.Models;

namespace Revit.Linter.PracticalTourPresenter.Infrastructure.Services;

/// <summary>Chooses the step a saved practical-tour session resumes from.</summary>
/// <remarks>
/// The run of the training check is the session's only checkpoint. Steps after it work with the findings
/// of that run, and those findings are not kept across a pause: a resumed session starts again at the run
/// step so it always explains fresh findings for the remaining tasks. Steps before the run are cheap,
/// document-independent actions, so they keep their saved position.
/// </remarks>
internal static class PracticalTourResume
{
    /// <summary>Returns the progress the session starts from.</summary>
    /// <param name="progress">The saved progress.</param>
    /// <param name="hasOpenDocument">Whether a document is open in Revit.</param>
    /// <returns>
    /// The saved progress, or a copy with the checkpoint step the session can actually continue from.
    /// </returns>
    internal static PracticalTourProgress ResolveStartProgress(
        PracticalTourProgress progress,
        bool hasOpenDocument)
    {
        PracticalTourStep step = ResolveStartStep(progress.CurrentStep, hasOpenDocument);
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
    /// <remarks>
    /// Without a document the session waits at the open-document step. A step after the run falls back to
    /// the run checkpoint, because the findings the saved step explained are gone; any earlier step is
    /// document-independent and stays as saved.
    /// </remarks>
    internal static PracticalTourStep ResolveStartStep(PracticalTourStep savedStep, bool hasOpenDocument)
    {
        if (!hasOpenDocument) return PracticalTourStep.OpenDocument;
        if (savedStep == PracticalTourStep.Completed) return savedStep;

        return IsAfterRunStep(savedStep) ? PracticalTourStep.RunDiagnostics : savedStep;
    }

    private static bool IsAfterRunStep(PracticalTourStep step)
    {
        int stepIndex = Array.IndexOf(PracticalTourStepCatalog.Steps, step);
        int runIndex = Array.IndexOf(PracticalTourStepCatalog.Steps, PracticalTourStep.RunDiagnostics);
        return stepIndex > runIndex;
    }
}
