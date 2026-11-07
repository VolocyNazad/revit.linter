using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions.Models;

namespace Revit.Linter.PracticalTourPresenter.Infrastructure.Services;

/// <summary>Advances the tour from the UI facts that the rest of the add-in publishes.</summary>
/// <remarks>
/// One switch maps every observed activity to the step it completes, so the question "what advances this
/// step" is answered in one place. Activities that carry a diagnostic code pass it through
/// <see cref="ObserveTrainingStep"/>: an action on another check never advances the tour.
/// </remarks>
internal sealed partial class PracticalTour
{
    private void ActivityStream_ActivityPublished(object? sender, UserInterfaceActivity activity)
    {
        try
        {
            switch (activity)
            {
                case DiagnosticRunCompletedActivity run:
                    ObserveTrainingRun(run);
                    break;
                case VisualizationAppliedActivity visualization:
                    ObserveTrainingVisualization(visualization);
                    break;
                case DiagnosticListFilteredActivity:
                    _viewModel?.Observe(PracticalTourStep.SearchAndFilters);
                    break;
                case DiagnosticSearchChangedActivity search:
                    _viewModel?.ObserveSearchText(search.SearchText);
                    break;
                case FixListPaneShownActivity:
                    _viewModel?.Observe(PracticalTourStep.FixList);
                    break;
                case FixSelectedActivity selectedFix:
                    ObserveTrainingStep(selectedFix.DiagnosticCode, PracticalTourStep.FixList);
                    break;
                case DiagnosticSelectionChangedActivity selection:
                    ObserveTrainingSelection(selection);
                    break;
                case FindingSelectedActivity finding:
                    ObserveTrainingStep(finding.DiagnosticCode, PracticalTourStep.InspectFinding);
                    break;
                case FixAppliedActivity applied:
                    ObserveTrainingStep(applied.DiagnosticCode, PracticalTourStep.UnderstandFix);
                    break;
                case IgnoreAppliedActivity ignored:
                    ObserveTrainingStep(ignored.DiagnosticCode, PracticalTourStep.UnderstandIgnore);
                    break;
                case IgnoreListPaneShownActivity:
                    _viewModel?.Observe(PracticalTourStep.IgnoreList);
                    break;
                case IgnoreSelectedActivity selectedEntry:
                    ObserveTrainingStep(selectedEntry.DiagnosticCode, PracticalTourStep.IgnoreList);
                    break;
                case DocumentationOpenedActivity:
                    _viewModel?.Observe(PracticalTourStep.OpenHelp);
                    break;
                case FindingNavigationUsedActivity navigation:
                    ObserveTrainingStep(navigation.DiagnosticCode, PracticalTourStep.NavigateFindings);
                    break;
                case RuleConfigurationOpenedActivity ruleConfiguration:
                    ObserveTrainingStep(ruleConfiguration.DiagnosticCode, PracticalTourStep.OpenConfigurationFolder);
                    break;
                case DiagnosticReportExportedActivity exported:
                    _viewModel?.ObserveReportExport(exported.Format);
                    break;
                case DocumentFocusChangedActivity:
                    EndSessionOnDocumentChange();
                    break;
                case DocumentOpenedActivity:
                    ObserveDocumentOpened();
                    break;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Practical tour was disabled while observing user activity");
            CloseAfterFailure();
        }
    }

    /// <summary>Credits a run only when it covered the training check alone.</summary>
    /// <remarks>
    /// A mixed run would credit findings from unrelated rules. The single-code check stays explicit
    /// because a list pattern would need System.Index, which the net48 WPF markup compilation lacks.
    /// </remarks>
    private void ObserveTrainingRun(DiagnosticRunCompletedActivity run)
    {
        if (run.Result == DiagnosticServiceResult.Failed) return;
        if (run.EnabledDiagnosticCodes.Length != 1) return;
        if (!IsPracticalTourDiagnostic(run.EnabledDiagnosticCodes[0])) return;

        _viewModel?.ObserveDiagnosticRun(run.FindingCount > 0);
    }

    private void ObserveTrainingVisualization(VisualizationAppliedActivity visualization)
    {
        if (!IsPracticalTourDiagnostic(visualization.DiagnosticCode)) return;

        _viewModel?.ObserveVisualization(
            visualization.SelectedFromMenu,
            visualization.AvailableOptionCount);
    }

    /// <summary>Credits a step only when the observed action belongs to the training check.</summary>
    /// <param name="diagnosticCode">The diagnostic the user acted on.</param>
    /// <param name="step">The step the action completes.</param>
    private void ObserveTrainingStep(string diagnosticCode, PracticalTourStep step)
    {
        if (!IsPracticalTourDiagnostic(diagnosticCode)) return;

        _viewModel?.Observe(step);
    }

    /// <summary>Completes the selection step only when the training check is the only enabled one.</summary>
    /// <remarks>
    /// Any other enabled diagnostic keeps the step open, so pre-existing selections cannot complete it.
    /// </remarks>
    private void ObserveTrainingSelection(DiagnosticSelectionChangedActivity selection)
    {
        if (!IsSinglePracticalTourDiagnostic(selection.ActiveDiagnosticCodes)) return;

        _viewModel?.Observe(PracticalTourStep.SelectDiagnostic);
    }

    /// <summary>Shows the panes a newly opened document needs, then advances or ends the tour.</summary>
    private void ObserveDocumentOpened()
    {
        tourHost.ShowPanes();
        if (!EndSessionOnDocumentChange())
            _viewModel?.Observe(PracticalTourStep.OpenDocument);
    }

    /// <summary>Ends the session when the user switches to another document.</summary>
    /// <returns><see langword="true"/> when the session ended.</returns>
    private bool EndSessionOnDocumentChange()
    {
        if (!EndOnDocumentChange()) return false;

        logger.LogInformation("Practical tour ended on document change at step {Step}", _viewModel?.CurrentStep);
        EndSession(hidePane: true);
        return true;
    }

    private static bool IsPracticalTourDiagnostic(string diagnosticCode)
        => string.Equals(diagnosticCode, DiagnosticCode, StringComparison.Ordinal);

    /// <summary>Returns whether the training diagnostic is the only enabled diagnostic.</summary>
    /// <param name="activeDiagnosticCodes">The enabled diagnostics after a selection change.</param>
    /// <remarks>
    /// The diagnostic-selection step completes only when every other check is off: a user who already had
    /// checks enabled cannot complete it by enabling the training check alongside them.
    /// </remarks>
    internal static bool IsSinglePracticalTourDiagnostic(IReadOnlyList<string> activeDiagnosticCodes) =>
        activeDiagnosticCodes.Count == 1 && IsPracticalTourDiagnostic(activeDiagnosticCodes[0]);

    // Findings, visualizations and fixes observed so far belong to the previous document, and the
    // shared TOUR001 check exists in every document, so continuing would advance the tour on
    // unrelated findings. Only waiting for a document survives a document change: opening one
    // is the awaited action itself.
    private bool EndOnDocumentChange()
    {
        if (_viewModel is null) return false;
        return _viewModel.CurrentStep != PracticalTourStep.OpenDocument;
    }
}
