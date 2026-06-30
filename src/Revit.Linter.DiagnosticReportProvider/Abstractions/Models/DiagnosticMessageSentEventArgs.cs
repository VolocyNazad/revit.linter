namespace Revit.Linter.DiagnosticReportProvider.Abstractions.Models;

public sealed class DiagnosticMessageSentEventArgs(DiagnosticReport report) : EventArgs
{
    public DiagnosticReport Report { get; } = report;
}
