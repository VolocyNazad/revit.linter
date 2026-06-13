namespace Revit.Linter.DiagnosticReportProvider.Abstractions.Models;

public sealed record DiagnosticReportMessage(string Format, params (string, object)[] Args);
