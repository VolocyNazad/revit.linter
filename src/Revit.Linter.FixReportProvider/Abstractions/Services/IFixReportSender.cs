using Revit.Linter.FixReportProvider.Abstractions.Models;

namespace Revit.Linter.FixReportProvider.Abstractions.Services;

/// <summary>
/// Publishes fix reports to registered receivers.
/// </summary>
public interface IFixReportSender
{
    /// <summary>
    /// Publishes the specified fix report.
    /// </summary>
    /// <param name="report">The fix report to publish.</param>
    void Send(FixReport report);
}
