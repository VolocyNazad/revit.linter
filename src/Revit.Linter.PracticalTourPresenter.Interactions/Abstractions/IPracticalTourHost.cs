using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions.Models;

namespace Revit.Linter.PracticalTourPresenter.Interactions.Abstractions;

/// <summary>
/// Supplies the facts and actions of the hosting add-in that the practical tour presents but does not own.
/// </summary>
/// <remarks>
/// The host implements this contract together with the separate welcome-wizard host contract. The tour
/// depends only on the pane, notification and diagnostics-filter actions it performs itself, so wizard-only
/// actions do not appear here.
/// </remarks>
public interface IPracticalTourHost
{
    /// <summary>Gets a value indicating whether Revit currently has an active project or family document.</summary>
    bool HasOpenDocument { get; }

    /// <summary>Gets a value indicating whether the active document shows findings of the given diagnostic.</summary>
    /// <param name="diagnosticCode">The diagnostic code, compared ordinally.</param>
    /// <returns>
    /// <see langword="true"/> when the report pane still displays at least one finding of the diagnostic for
    /// the active document; <see langword="false"/> when no document is open or the report holds no such finding.
    /// </returns>
    /// <remarks>
    /// A resumed tour continues at a finding-specific step only when the findings it explains are still
    /// there; otherwise it starts from the run step.
    /// </remarks>
    bool HasDiagnosticFindings(string diagnosticCode);

    /// <summary>Shows the add-in dockable panes that are currently hidden.</summary>
    /// <remarks>Requires a valid Revit API context.</remarks>
    void ShowPanes();

    /// <summary>Queues the specified add-in pane to be shown and brought to the foreground.</summary>
    /// <remarks>The host performs the Revit UI operation in a valid API context.</remarks>
    void ShowPane(OnboardingPane pane);

    /// <summary>Shows the dockable pane that contains the practical tour.</summary>
    void ShowPracticalTourPane();

    /// <summary>Hides the dockable pane that contains the practical tour.</summary>
    void HidePracticalTourPane();

    /// <summary>Shows a localized notification after the practical tour is completed.</summary>
    void ShowPracticalTourCompletedNotification();

    /// <summary>Clears the diagnostics pane search and re-enables its filters.</summary>
    void ResetDiagnosticsSearchAndFilters();
}
