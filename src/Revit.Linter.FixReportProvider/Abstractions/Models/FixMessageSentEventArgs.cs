namespace Revit.Linter.FixReportProvider.Abstractions.Models;

public sealed class FixMessageSentEventArgs(FixReport report) : EventArgs
{
    public FixReport Report { get; } = report;
}
