using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

public interface IDiagnosticCatalogSnapshotLease : IDisposable
{
    long Version { get; }
    DiagnosticCatalogSnapshot Snapshot { get; }
}
