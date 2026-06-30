using Revit.Linter.FixReportProvider.Abstractions.Models;

namespace Revit.Linter.FixReportProvider.Abstractions.Services;

public interface IFixReportSender
{
    void Send(FixReport report);
}
