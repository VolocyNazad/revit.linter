namespace Revit.Linter.DiagnosticReportPresenter.Services;

/// <summary>
/// Decides when the diagnostic report list is refreshed after model or ignore changes.
/// </summary>
/// <remarks>
/// A run waits for a quiet period so a burst of changes such as an undo chain is checked once, and
/// document diagnostics run less often than element checks because they scan the whole document.
/// The scheduler only decides what to check and when; the report executes the batches and updates
/// the rows.
/// </remarks>
internal interface IDiagnosticRefreshScheduler
{
    /// <summary>Raised in a valid Revit API context with the checks that are due.</summary>
    event EventHandler<DiagnosticRefreshRequestedEventArgs>? RefreshRequested;

    /// <summary>Queues elements for the next run and the document for its document-level refresh.</summary>
    /// <param name="document">The document to check.</param>
    /// <param name="elementIds">The elements to check again; empty when only the document refresh applies.</param>
    void QueueRefresh(Document document, IReadOnlyCollection<ElementId> elementIds);

    /// <summary>Remembers elements whose rows an ignore removed, so a later undo or removal can restore them.</summary>
    /// <param name="document">The document the ignore belongs to.</param>
    /// <param name="elementIds">The elements whose rows were removed.</param>
    void RememberIgnoredElements(Document document, IReadOnlyCollection<ElementId> elementIds);

    /// <summary>Gets the elements whose rows an ignore removed from the report.</summary>
    /// <param name="document">The document the ignore belongs to.</param>
    /// <returns>The remembered elements, or an empty collection when none were ignored.</returns>
    IReadOnlyCollection<ElementId> GetIgnoredElements(Document document);

    /// <summary>Stops or resumes the scheduled runs and drops whatever is queued.</summary>
    /// <param name="isPaused">Whether the report is paused.</param>
    void SetPaused(bool isPaused);

    /// <summary>Drops the queued checks and the timer, for example when the pane closes.</summary>
    void ClearPending();
}
