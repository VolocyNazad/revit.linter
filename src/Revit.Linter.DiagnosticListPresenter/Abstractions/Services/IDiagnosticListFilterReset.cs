namespace Revit.Linter.DiagnosticListPresenter.Abstractions.Services;

/// <summary>
/// Resets the diagnostics pane search and filters to their initial state.
/// </summary>
public interface IDiagnosticListFilterReset
{
    /// <summary>Clears the search box and re-enables every filter without publishing user activity.</summary>
    void ResetSearchAndFilters();
}
