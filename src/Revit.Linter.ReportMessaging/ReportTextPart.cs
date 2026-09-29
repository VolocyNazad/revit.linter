namespace Revit.Linter.ReportMessaging;

/// <summary>
/// Represents one renderable part of a report message.
/// </summary>
/// <param name="Type">The rendering behavior for the part.</param>
/// <param name="Text">The text displayed for the part.</param>
/// <param name="Data">Optional data associated with an interactive part.</param>
public sealed record ReportTextPart(ReportInlineType Type, string Text, object? Data = null);
