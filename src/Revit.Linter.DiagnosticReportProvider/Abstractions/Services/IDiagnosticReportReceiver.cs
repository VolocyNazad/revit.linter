using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;

namespace Revit.Linter.DiagnosticReportProvider.Abstractions.Services;

public interface IDiagnosticReportReceiver
{
    public event DiagnosticReportHandler? DiagnosticReportSent;
}
