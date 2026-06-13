namespace Revit.Linter.DiagnosticReportProvider.Abstractions.Models;

public sealed class MessageSentEventArgs(DiagnosticReport report) : EventArgs
{
    public DiagnosticReport Report { get; } = report;
}
