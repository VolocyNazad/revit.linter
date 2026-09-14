namespace Revit.Linter.FixReportProvider.Abstractions.Models;

/// <summary>
/// Fix report
/// </summary>
/// <param name="Code"> Diagnostic code </param>
/// <param name="DocumentTitle"> Document title </param>
/// <param name="Message"> Fix results message </param>
public sealed record FixReport(string Code, string DocumentTitle, FixReportMessage Message)
{
    public DateTime Created {  get; } = DateTime.Now;
}
