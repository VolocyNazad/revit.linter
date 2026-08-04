using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

public interface IDiagnosticCatalog
{
    event EventHandler<DiagnosticCatalogChangedEventArgs>? Changed;
    event EventHandler<DiagnosticCatalogRefreshFailedEventArgs>? RefreshFailed;

    IDiagnosticCatalogSnapshotLease AcquireSnapshot();
    void Refresh();
}
