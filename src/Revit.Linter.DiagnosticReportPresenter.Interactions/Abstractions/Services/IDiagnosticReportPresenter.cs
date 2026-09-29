namespace Revit.Linter.DiagnosticReportPresenter.Interactions.Abstractions.Services;

/// <summary>
/// Controls the diagnostic reports displayed by the report presenter.
/// </summary>
public interface IDiagnosticReportPresenter
{
    /// <summary>
    /// Removes all displayed diagnostic reports.
    /// </summary>
    void Clear();

    /// <summary>
    /// Removes displayed diagnostic reports associated with the specified document.
    /// </summary>
    /// <param name="documentTitle">The title of the document whose reports are removed.</param>
    void Clear(string documentTitle);

    /// <summary>
    /// Refreshes the displayed diagnostic reports.
    /// </summary>
    void Refresh();
}
