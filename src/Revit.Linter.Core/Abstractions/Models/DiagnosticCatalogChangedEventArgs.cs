namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Describes a successfully published diagnostic catalog revision.</summary>
/// <param name="version">The monotonically increasing catalog version.</param>
/// <param name="origin">The source that initiated the catalog change.</param>
public sealed class DiagnosticCatalogChangedEventArgs(
    long version,
    DiagnosticCatalogChangeOrigin origin) : EventArgs
{
    /// <summary>Gets the published catalog version.</summary>
    public long Version { get; } = version;
    /// <summary>Gets the source that initiated the change.</summary>
    public DiagnosticCatalogChangeOrigin Origin { get; } = origin;
}
