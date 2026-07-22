using Revit.Linter.FixReportProvider.Abstractions.Models;
using Revit.Linter.FixReportProvider.Abstractions.Services;

namespace Revit.Linter.FixReportProvider.Services;

internal sealed class FixReportProvider : IFixReportReceiver, IFixReportSender
{
    public event FixReportHandler? ReportSent;

    public void Send(FixReport report) => ReportSent?.Invoke(this, new(report));
}
