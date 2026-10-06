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
    /// <remarks>
    /// Clearing also restores an active visualization. That needs a Revit API context; outside one the
    /// reports are still removed and the visualization is restored on the next Idling event.
    /// </remarks>
    void Clear(string documentTitle);

    /// <summary>
    /// Refreshes the displayed diagnostic reports and counts the findings for a document.
    /// </summary>
    /// <param name="documentTitle">The title of the document whose findings are counted after refresh.</param>
    /// <returns>The number of matching findings currently presented.</returns>
    int Refresh(string documentTitle);
}
