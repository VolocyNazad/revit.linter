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
}
