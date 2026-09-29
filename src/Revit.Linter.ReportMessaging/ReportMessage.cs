namespace Revit.Linter.ReportMessaging;

/// <summary>
/// Represents a parsed report message and its plain-text equivalent.
/// </summary>
/// <param name="Parts">The ordered message parts used for rich rendering.</param>
/// <param name="Text">The complete message without rich formatting.</param>
public sealed record ReportMessage(IReadOnlyList<ReportTextPart> Parts, string Text);
