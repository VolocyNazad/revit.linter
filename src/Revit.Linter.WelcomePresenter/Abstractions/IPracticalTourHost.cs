using Revit.Linter.WelcomePresenter.Abstractions.Models;

namespace Revit.Linter.WelcomePresenter.Abstractions;

/// <summary>
/// Supplies the facts and actions of the hosting add-in that the practical tour presents but does not own.
/// </summary>
/// <remarks>
/// The host implements this contract together with <see cref="IWelcomeHost"/>. The tour depends only on the
/// pane, notification and diagnostics-filter actions it performs itself, so wizard-only actions do not appear
/// here.
/// </remarks>
public interface IPracticalTourHost
{
    /// <summary>Gets a value indicating whether Revit currently has an active project or family document.</summary>
    bool HasOpenDocument { get; }

    /// <summary>Shows the add-in dockable panes that are currently hidden.</summary>
    /// <remarks>Requires a valid Revit API context.</remarks>
    void ShowPanes();

    /// <summary>Queues the specified add-in pane to be shown and brought to the foreground.</summary>
    /// <remarks>The host performs the Revit UI operation in a valid API context.</remarks>
    void ShowPane(WelcomePane pane);

    /// <summary>Shows the dockable pane that contains the practical tour.</summary>
    void ShowPracticalTourPane();

    /// <summary>Hides the dockable pane that contains the practical tour.</summary>
    void HidePracticalTourPane();

    /// <summary>Shows a localized notification after the practical tour is completed.</summary>
    void ShowPracticalTourCompletedNotification();

    /// <summary>Clears the diagnostics pane search and re-enables its filters.</summary>
    void ResetDiagnosticsSearchAndFilters();
}
