using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Publishes versioned, leased snapshots of the currently registered diagnostics.</summary>
public interface IDiagnosticCatalog
{
    /// <summary>Occurs after a refreshed catalog revision has been published.</summary>
    event EventHandler<DiagnosticCatalogChangedEventArgs>? Changed;
    /// <summary>Occurs when a refresh fails and the previous revision remains active.</summary>
    event EventHandler<DiagnosticCatalogRefreshFailedEventArgs>? RefreshFailed;

    /// <summary>Acquires the current catalog revision and keeps its owned registrations alive.</summary>
    /// <returns>A lease that must be disposed after the snapshot is no longer used.</returns>
    IDiagnosticCatalogSnapshotLease AcquireSnapshot();
    /// <summary>Rebuilds the catalog from its registration providers.</summary>
    void Refresh();
}
