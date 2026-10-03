using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;

namespace Revit.Linter.DiagnosticReportProvider.Abstractions.Services;

/// <summary>
/// Exposes notifications about diagnostic reports.
/// </summary>
public interface IDiagnosticReportReceiver
{
    /// <summary>
    /// Occurs when a diagnostic report is sent.
    /// </summary>
    event DiagnosticReportHandler? ReportSent;

    /// <summary>
    /// Occurs when several diagnostic reports are sent together.
    /// </summary>
    /// <remarks>
    /// A receiver that handles both events gets every report exactly once: a batch is raised only through
    /// this event while it has subscribers.
    /// </remarks>
    event EventHandler<DiagnosticReportsSentEventArgs>? ReportsSent;
}
