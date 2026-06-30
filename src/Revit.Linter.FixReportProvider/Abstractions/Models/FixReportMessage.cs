namespace Revit.Linter.FixReportProvider.Abstractions.Models;

public sealed record FixReportMessage(string Format, params (string, object)[] Args);
