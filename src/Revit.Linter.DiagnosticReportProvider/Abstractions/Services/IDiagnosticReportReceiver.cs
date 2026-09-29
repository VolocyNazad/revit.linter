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
}
