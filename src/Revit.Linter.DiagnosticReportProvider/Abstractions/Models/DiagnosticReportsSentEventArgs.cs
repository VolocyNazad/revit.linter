namespace Revit.Linter.DiagnosticReportProvider.Abstractions.Models;

/// <summary>
/// Provides data for a notification about several diagnostic reports sent together.
/// </summary>
/// <param name="reports">The diagnostic reports that were sent, in the order they were produced.</param>
public sealed class DiagnosticReportsSentEventArgs(IReadOnlyList<DiagnosticReport> reports) : EventArgs
{
    /// <summary>
    /// Gets the diagnostic reports that were sent, in the order they were produced.
    /// </summary>
    public IReadOnlyList<DiagnosticReport> Reports { get; } = reports;
}