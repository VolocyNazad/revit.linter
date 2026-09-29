namespace Revit.Linter.FixReportProvider.Abstractions.Models;

/// <summary>
/// Describes a fix-result message format and its named arguments.
/// </summary>
/// <param name="Format">The message format.</param>
/// <param name="Args">The named values used to render the message.</param>
public sealed record FixReportMessage(string Format, params (string, object)[] Args);
