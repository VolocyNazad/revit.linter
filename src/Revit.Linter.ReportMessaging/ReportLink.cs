namespace Revit.Linter.ReportMessaging;

/// <summary>
/// Describes an interactive value embedded in a report message.
/// </summary>
/// <param name="Text">The text displayed for the link.</param>
/// <param name="Data">The value supplied as the link command parameter.</param>
public sealed record ReportLink(string Text, object? Data);
