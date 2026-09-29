namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Describes a diagnostic catalog refresh that could not be published.</summary>
/// <param name="exception">The exception that prevented the refresh.</param>
public sealed class DiagnosticCatalogRefreshFailedEventArgs(Exception exception) : EventArgs
{
    /// <summary>Gets the exception that prevented the refresh.</summary>
    public Exception Exception { get; } = exception;
}
