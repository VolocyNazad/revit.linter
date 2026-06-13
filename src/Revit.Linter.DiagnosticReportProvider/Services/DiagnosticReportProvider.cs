using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Services;

namespace Revit.Linter.DiagnosticReportProvider.Services;

internal sealed class DiagnosticReportProvider : IDiagnosticReportReceiver, IDiagnosticReportSender
{
    public event DiagnosticReportHandler? DiagnosticReportSent;

    public void Send(DiagnosticReport report) => DiagnosticReportSent?.Invoke(this, new(report));
}
