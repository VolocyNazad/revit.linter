namespace Revit.Linter.DiagnosticReportPresenter.Exporting;

internal sealed record DiagnosticReportExportItem(
    string Severity,
    string SeverityDisplayName,
    string Code,
    string Message,
    string Document,
    DateTimeOffset Created,
    bool IsObsolete,
    string? ObsoleteDescription);
