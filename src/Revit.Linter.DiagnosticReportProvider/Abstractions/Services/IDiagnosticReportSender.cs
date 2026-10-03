using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;

namespace Revit.Linter.DiagnosticReportProvider.Abstractions.Services;

/// <summary>
/// Publishes diagnostic reports to registered receivers.
/// </summary>
public interface IDiagnosticReportSender
{
    /// <summary>
    /// Publishes the specified diagnostic report.
    /// </summary>
    /// <param name="report">The diagnostic report to publish.</param>
    void Send(DiagnosticReport report);

    /// <summary>
    /// Publishes several diagnostic reports as one notification.
    /// </summary>
    /// <param name="reports">The diagnostic reports to publish, in the order they were produced.</param>
    /// <remarks>
    /// Receivers subscribed to <see cref="IDiagnosticReportReceiver.ReportsSent"/> get the reports as one
    /// batch. When nobody is subscribed to it, every report is published separately through
    /// <see cref="IDiagnosticReportReceiver.ReportSent"/>, so no report is lost either way.
    /// </remarks>
    void SendMany(IReadOnlyList<DiagnosticReport> reports);
}
