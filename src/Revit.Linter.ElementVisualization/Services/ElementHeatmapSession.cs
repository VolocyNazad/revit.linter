using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementVisualization.Services;

/// <summary>Owns the graphics of one applied findings heat map and restores them.</summary>
internal sealed class ElementHeatmapSession(
    Document document,
    IReadOnlyList<IRestoreSession> sessions,
    int coloredElementCount,
    int skippedElementCount,
    ILogger logger) : IElementHeatmapSession
{
    private bool _restored;

    /// <inheritdoc />
    public int ColoredElementCount { get; } = coloredElementCount;

    /// <inheritdoc />
    public int SkippedElementCount { get; } = skippedElementCount;

    /// <inheritdoc />
    public void Restore()
    {
        if (_restored || sessions.Count == 0)
        {
            _restored = true;
            return;
        }

        if (!document.IsValidObject)
        {
            // The document was closed: the graphics it stored are gone with it.
            _restored = true;
            return;
        }

        // The sessions are restored in reverse order of their application: the background overrides
        // disappear before the category visibility, so no element briefly keeps a stale transparency.
        using TransactionGroup? transactionGroup = VisualizationTransactions.StartGroup(
            document, ElementVisualizationLocalizations.GetString("heatmapRestore_transactionGroup"));
        List<Exception> errors = [];
        for (int index = sessions.Count - 1; index >= 0; index--)
        {
            try
            {
                sessions[index].Restore();
            }
            catch (Autodesk.Revit.Exceptions.InvalidObjectException exception)
            {
                // The view or its elements were deleted or their creation was undone: the state to
                // restore no longer exists, so the session counts as restored instead of failing on
                // every retry.
                logger.LogWarning(
                    exception,
                    "The findings heat map session refers to deleted objects; treating it as restored");
            }
            catch (Exception exception)
            {
                errors.Add(new InvalidOperationException(
                    $"Failed to restore findings heat map session {index + 1}.", exception));
            }
        }

        VisualizationTransactions.Assimilate(transactionGroup);
        if (errors.Count > 0)
            throw new AggregateException("Failed to restore the findings heat map.", errors);

        _restored = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_restored)
        {
            try
            {
                Restore();
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Failed to restore the findings heat map while disposing it");
            }
        }

        foreach (IRestoreSession session in sessions)
        {
            try
            {
                session.Dispose();
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Failed to dispose the findings heat map graphics session");
            }
        }
    }
}
