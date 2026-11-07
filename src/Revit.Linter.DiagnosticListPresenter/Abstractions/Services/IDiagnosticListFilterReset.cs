namespace Revit.Linter.DiagnosticListPresenter.Abstractions.Services;

/// <summary>
/// Resets the diagnostics pane search, filters and sort order to their initial state.
/// </summary>
public interface IDiagnosticListFilterReset
{
    /// <summary>
    /// Clears the search box, re-enables every filter and restores the default sort order without
    /// publishing user activity.
    /// </summary>
    void ResetSearchAndFilters();
}
