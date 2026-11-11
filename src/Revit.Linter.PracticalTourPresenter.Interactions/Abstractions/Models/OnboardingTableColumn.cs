namespace Revit.Linter.PracticalTourPresenter.Interactions.Abstractions.Models;

/// <summary>
/// Identifies a table column that a guided step may show while the step lasts.
/// </summary>
/// <remarks>
/// Every member names the table and the column it belongs to, so the tour does not have to know the
/// stored table and column identifiers. The host reveals the column for the duration of the step and
/// restores the visibility the user chose afterwards.
/// </remarks>
public enum OnboardingTableColumn
{
    /// <summary>The checkbox that turns a diagnostic on or off in the diagnostics table.</summary>
    DiagnosticActive,
}
