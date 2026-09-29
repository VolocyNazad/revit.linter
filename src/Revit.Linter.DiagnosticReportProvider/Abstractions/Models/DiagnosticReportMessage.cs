namespace Revit.Linter.DiagnosticReportProvider.Abstractions.Models;

/// <summary>
/// Describes a diagnostic message format and its named arguments.
/// </summary>
/// <param name="Format">The message format.</param>
/// <param name="Args">The named values used to render the message.</param>
public sealed record DiagnosticReportMessage(string Format, params (string, object)[] Args);
