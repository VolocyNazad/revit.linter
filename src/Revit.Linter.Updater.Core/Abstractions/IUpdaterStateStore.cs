using Revit.Linter.Updater.Core.Models;

namespace Revit.Linter.Updater.Core.Abstractions;

/// <summary>Persists per-user updater state independently of machine policy.</summary>
public interface IUpdaterStateStore
{
    /// <summary>Loads state, returning defaults when the state file does not exist.</summary>
    Task<UpdaterState> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Atomically replaces the persisted state.</summary>
    Task SaveAsync(UpdaterState state, CancellationToken cancellationToken = default);
}
