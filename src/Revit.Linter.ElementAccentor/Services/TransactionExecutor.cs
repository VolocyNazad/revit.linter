namespace Revit.Linter.ElementAccentor.Services;

internal static class TransactionExecutor
{
    public static void Execute(Document document, string name, Action action)
    {
        if (document.IsModifiable)
        {
            action();
            return;
        }

        using Transaction transaction = new(document, name);
        if (transaction.Start() != TransactionStatus.Started)
            throw new InvalidOperationException($"Failed to start transaction '{name}'.");
        try
        {
            action();
            if (transaction.Commit() != TransactionStatus.Committed)
                throw new InvalidOperationException($"Failed to commit transaction '{name}'.");
        }
        catch
        {
            if (transaction.GetStatus() == TransactionStatus.Started)
                transaction.RollBack();
            throw;
        }
    }
}
