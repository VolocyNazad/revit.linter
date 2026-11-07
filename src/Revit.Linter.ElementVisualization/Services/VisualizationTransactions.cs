namespace Revit.Linter.ElementVisualization.Services;

/// <summary>Starts, commits and rolls back the transactions a visualization owns.</summary>
/// <remarks>
/// A visualization keeps the document unchanged for the user between the two commits a pipeline may
/// need, so these helpers are the only place that knows the transaction rules.
/// </remarks>
internal static class VisualizationTransactions
{
    /// <summary>Starts a transaction group that makes the pipeline atomic.</summary>
    /// <param name="document">The document the visualization changes.</param>
    /// <param name="name">The group name that becomes the single undo item.</param>
    /// <returns>
    /// The started group, or <see langword="null"/> when an outer transaction already owns the
    /// document, in which case the caller only contributes to that transaction.
    /// </returns>
    public static TransactionGroup? StartGroup(Document document, string name)
    {
        if (document.IsModifiable) return null;

        TransactionGroup group = new(document, name);
        if (group.Start() == TransactionStatus.Started) return group;

        group.Dispose();
        throw new InvalidOperationException($"Failed to start transaction group '{name}'.");
    }

    /// <summary>Merges the committed step transactions into the single undo item of the group.</summary>
    public static void Assimilate(TransactionGroup? group)
    {
        if (group is null) return;
        if (group.Assimilate() != TransactionStatus.Committed)
            throw new InvalidOperationException($"Failed to assimilate transaction group '{group.GetName()}'.");
    }

    /// <summary>Discards the changes of the group, if it is still open.</summary>
    public static void RollBack(TransactionGroup? group)
    {
        if (group is null || group.GetStatus() != TransactionStatus.Started) return;
        group.RollBack();
    }

    /// <summary>Discards the changes of the step transaction, if it is still open.</summary>
    public static void RollBack(Transaction? transaction)
    {
        if (transaction is null) return;
        try
        {
            if (transaction.GetStatus() == TransactionStatus.Started)
                transaction.RollBack();
        }
        finally
        {
            transaction.Dispose();
        }
    }
}
