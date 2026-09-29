namespace Revit.Linter.DiagnosticReportProvider.Abstractions.Models;

/// <summary>
/// Provides data for a diagnostic-report notification.
/// </summary>
/// <param name="report">The diagnostic report that was sent.</param>
public sealed class DiagnosticMessageSentEventArgs(DiagnosticReport report) : EventArgs
{
    /// <summary>
    /// Gets the diagnostic report that was sent.
    /// </summary>
    public DiagnosticReport Report { get; } = report;
}
