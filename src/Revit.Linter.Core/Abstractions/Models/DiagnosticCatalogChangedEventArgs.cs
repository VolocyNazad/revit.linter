namespace Revit.Linter.Core.Abstractions.Models;

public sealed class DiagnosticCatalogChangedEventArgs(
    long version,
    DiagnosticCatalogChangeOrigin origin) : EventArgs
{
    public long Version { get; } = version;
    public DiagnosticCatalogChangeOrigin Origin { get; } = origin;
}
