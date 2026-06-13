using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;

namespace Revit.Linter.DiagnosticReportProvider.Abstractions.Services;

public interface IDiagnosticReportSender
{
    void Send(DiagnosticReport report);
}
