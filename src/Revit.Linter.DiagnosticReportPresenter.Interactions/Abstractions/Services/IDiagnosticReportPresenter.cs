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

    /// <summary>Starts or stops updating the displayed reports when the document changes.</summary>
    /// <param name="isEnabled">
    /// <see langword="true"/> to check changed elements again and replace their rows;
    /// <see langword="false"/> to keep the displayed rows as they are.
    /// </param>
    /// <remarks>
    /// A stopped refresh leaves the rows visible while the document changes. The changes made meanwhile
    /// are not remembered: when the refresh starts again, only later changes reach the report.
    /// </remarks>
    void SetDocumentChangesRefresh(bool isEnabled);

    /// <summary>Removes the displayed findings of one diagnostic from every document.</summary>
    /// <param name="diagnosticCode">The diagnostic code, compared ordinally.</param>
    /// <remarks>
    /// Used by a guided session that clears its own training findings when the user pauses it; the
    /// findings of other diagnostics stay untouched. An active visualization of that diagnostic is
    /// restored, because the rows that offered it are gone.
    /// </remarks>
    void ClearFindings(string diagnosticCode);
}
