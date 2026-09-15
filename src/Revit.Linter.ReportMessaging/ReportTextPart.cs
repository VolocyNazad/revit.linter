namespace Revit.Linter.ReportMessaging;

public sealed record ReportTextPart(ReportInlineType Type, string Text, object? Data = null);
