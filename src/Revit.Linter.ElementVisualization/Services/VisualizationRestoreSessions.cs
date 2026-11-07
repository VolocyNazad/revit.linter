using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementVisualization.Services;

/// <summary>Keeps the restore sessions of an applied pipeline and restores them in reverse order.</summary>
/// <remarks>
/// A session can fail to restore because its objects were deleted or undone; the store classifies
/// that expected case as restored, retains unexpected failures for a later retry, and reports a
/// repeated failure only once instead of on every trigger.
/// </remarks>
internal sealed class VisualizationRestoreSessions(
    string pipelineName,
    string diagnosticCode,
    ILogger logger)
{
    private readonly List<IRestoreSession> _sessions = [];
    private Document? _sessionDocument;
    private int _consecutiveRestoreFailures;

    /// <summary>Stores the sessions of a successful run.</summary>
    /// <param name="document">The document the sessions restore.</param>
    /// <param name="sessions">The sessions created by the applied steps.</param>
    public void Add(Document document, IEnumerable<IRestoreSession> sessions)
    {
        _sessions.AddRange(sessions);
        _sessionDocument = document;
        _consecutiveRestoreFailures = 0;
    }

    /// <summary>Restores every stored session in reverse order and drops the restored ones.</summary>
    /// <exception cref="System.InvalidOperationException">At least one session could not be restored; it stays stored for a retry.</exception>
    public void Restore()
    {
        int sessionCount = _sessions.Count;
        if (sessionCount > 0)
            logger.LogInformation(
                "Restoring visualization pipeline {VisualizationName} for diagnostic {DiagnosticCode}. " +
                "Sessions: {SessionCount}",
                pipelineName, diagnosticCode, sessionCount);

        // A closed document invalidates the stored Document wrapper, and starting a transaction
        // group from it would throw before the sessions are classified. The sessions restore
        // nothing without their document, so they are restored individually instead.
        Document? sessionDocument = _sessionDocument;
        using TransactionGroup? transactionGroup = sessionDocument is { IsValidObject: true }
            ? VisualizationTransactions.StartGroup(
                sessionDocument,
                ElementVisualizationLocalizations.GetString("restoreVisualization_transactionGroup", pipelineName))
            : null;
        List<IRestoreSession> restoredSessions = [];
        List<Exception> errors = [];
        for (int index = _sessions.Count - 1; index >= 0; index--)
        {
            IRestoreSession session = _sessions[index];
            try
            {
                session.Restore();
            }
            catch (Autodesk.Revit.Exceptions.InvalidObjectException exception)
            {
                // The Revit objects this session compensates no longer exist: they were deleted, or
                // their creation was undone (for example the user undid the visualization view
                // creation). The state to restore went with them, so the session counts as restored
                // instead of failing identically on every retry.
                logger.LogWarning(
                    exception,
                    "Visualization session {SessionNumber} of pipeline {VisualizationName} for " +
                    "diagnostic {DiagnosticCode} refers to deleted objects; treating it as restored",
                    index + 1, pipelineName, diagnosticCode);
            }
            catch (Exception exception)
            {
                errors.Add(new InvalidOperationException(
                    $"Failed to restore visualization session {index + 1}.", exception));
                continue;
            }
            restoredSessions.Add(session);
        }
        VisualizationTransactions.Assimilate(transactionGroup);

        foreach (IRestoreSession session in restoredSessions)
        {
            _sessions.Remove(session);
            try
            {
                session.Dispose();
            }
            catch (Exception exception)
            {
                // A session classified as restored above keeps its delegate, so disposing it repeats
                // the same failure. Disposal is best-effort, as in the pipeline's Dispose(bool).
                logger.LogDebug(
                    exception,
                    "Failed to dispose a visualization session of pipeline {VisualizationName} for " +
                    "diagnostic {DiagnosticCode}", pipelineName, diagnosticCode);
            }
        }

        if (errors.Count > 0)
        {
            AggregateException error = new(errors);
            // The first failure of a streak is unexpected; repeats of the same stuck sessions would
            // only spam the log (a poisoned API context or a permanently unrestorable session fails
            // every trigger identically), so they are warnings. A success or a new apply starts
            // a new streak. The exception stays attached either way.
            _consecutiveRestoreFailures++;
            if (_consecutiveRestoreFailures == 1)
                logger.LogError(
                    error,
                    "Failed to restore {FailedSessionCount} visualization sessions in pipeline " +
                    "{VisualizationName} for diagnostic {DiagnosticCode}; sessions retained for retry",
                    errors.Count, pipelineName, diagnosticCode);
            else
                logger.LogWarning(
                    error,
                    "Still failing to restore {FailedSessionCount} visualization sessions in pipeline " +
                    "{VisualizationName} for diagnostic {DiagnosticCode}; sessions retained for retry",
                    errors.Count, pipelineName, diagnosticCode);
            throw new InvalidOperationException("Failed to restore the previous visualization state.", error);
        }

        _consecutiveRestoreFailures = 0;
        _sessionDocument = null;

        if (sessionCount > 0)
            logger.LogInformation(
                "Visualization pipeline {VisualizationName} for diagnostic {DiagnosticCode} restored",
                pipelineName, diagnosticCode);
    }
}
