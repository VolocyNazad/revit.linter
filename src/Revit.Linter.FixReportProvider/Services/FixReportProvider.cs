using Revit.Linter.FixReportProvider.Abstractions.Models;
using Revit.Linter.FixReportProvider.Abstractions.Services;

namespace Revit.Linter.FixReportProvider.Services;

internal sealed class FixReportProvider : IFixReportReceiver, IFixReportSender
{
    public event FixReportHandler? FixReportSent;

    public void Send(FixReport report) => FixReportSent?.Invoke(this, new(report));
}
