namespace Revit.Linter.FixReportProvider.Abstractions.Models;

/// <summary>
/// Describes the result of applying a diagnostic fix.
/// </summary>
/// <param name="Code">The code of the diagnostic whose fix was applied.</param>
/// <param name="DocumentTitle">The title of the document in which the fix was applied.</param>
/// <param name="Message">The message describing the fix result.</param>
public sealed record FixReport(string Code, string DocumentTitle, FixReportMessage Message)
{
    /// <summary>
    /// Gets the local time at which the report was created.
    /// </summary>
    public DateTime Created {  get; } = DateTime.Now;
}
