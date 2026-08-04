namespace Revit.Linter.Core.Abstractions.Models;

public sealed class DiagnosticCatalogRefreshFailedEventArgs(Exception exception) : EventArgs
{
    public Exception Exception { get; } = exception;
}
