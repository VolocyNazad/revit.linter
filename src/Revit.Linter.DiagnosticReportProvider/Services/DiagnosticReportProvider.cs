using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Services;

namespace Revit.Linter.DiagnosticReportProvider.Services;

internal sealed class DiagnosticReportProvider : IDiagnosticReportReceiver, IDiagnosticReportSender
{
    public event DiagnosticReportHandler? ReportSent;

    public event EventHandler<DiagnosticReportsSentEventArgs>? ReportsSent;

    public void Send(DiagnosticReport report) => ReportSent?.Invoke(this, new(report));

    public void SendMany(IReadOnlyList<DiagnosticReport> reports)
    {
        if (reports.Count == 0) return;

        if (ReportsSent is { } batchHandler)
        {
            batchHandler.Invoke(this, new DiagnosticReportsSentEventArgs(reports));
            return;
        }

        // Nobody handles batches, so the reports reach the single-report receivers one by one.
        foreach (DiagnosticReport report in reports)
            Send(report);
    }
}
