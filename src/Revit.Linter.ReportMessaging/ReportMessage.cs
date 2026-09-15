namespace Revit.Linter.ReportMessaging;

public sealed record ReportMessage(IReadOnlyList<ReportTextPart> Parts, string Text);
