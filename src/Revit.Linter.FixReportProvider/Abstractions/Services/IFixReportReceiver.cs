using Revit.Linter.FixReportProvider.Abstractions.Models;

namespace Revit.Linter.FixReportProvider.Abstractions.Services;

/// <summary>
/// Exposes notifications about fix reports.
/// </summary>
public interface IFixReportReceiver
{
    /// <summary>
    /// Occurs when a fix report is sent.
    /// </summary>
    event FixReportHandler? ReportSent;
}
