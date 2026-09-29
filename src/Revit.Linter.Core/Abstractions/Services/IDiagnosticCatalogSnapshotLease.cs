using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Keeps one diagnostic catalog revision and its owned registrations alive.</summary>
public interface IDiagnosticCatalogSnapshotLease : IDisposable
{
    /// <summary>Gets the catalog revision represented by this lease.</summary>
    long Version { get; }
    /// <summary>Gets the immutable snapshot owned by this lease.</summary>
    DiagnosticCatalogSnapshot Snapshot { get; }
}
