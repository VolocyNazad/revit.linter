using Revit.Linter.FixReportProvider.Abstractions.Models;

namespace Revit.Linter.FixReportProvider.Abstractions.Services;

public interface IFixReportReceiver
{
    public event FixReportHandler? FixReportSent;
}
