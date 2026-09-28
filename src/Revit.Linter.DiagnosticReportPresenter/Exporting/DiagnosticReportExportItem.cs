namespace Revit.Linter.DiagnosticReportPresenter.Exporting;

internal sealed record DiagnosticReportExportItem(
    string Severity,
    string Code,
    string Message,
    string Document,
    DateTime Created);
