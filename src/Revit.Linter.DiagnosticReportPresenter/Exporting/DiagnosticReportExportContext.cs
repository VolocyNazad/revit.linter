using System.Globalization;

namespace Revit.Linter.DiagnosticReportPresenter.Exporting;

internal sealed class DiagnosticReportExportContext
{
    public required CultureInfo Culture { get; init; }
    public required CultureInfo UiCulture { get; init; }
    public required string DocumentTitle { get; init; }
    public required DateTime ExportedAt { get; init; }
    public required string SeverityHeader { get; init; }
    public required string CodeHeader { get; init; }
    public required string MessageHeader { get; init; }
    public required string DocumentHeader { get; init; }
    public required string CreatedHeader { get; init; }
    public required string ReportTitle { get; init; }
    public required string GeneratedLabel { get; init; }
    public required string TotalLabel { get; init; }
    public required string SummaryByCodeTitle { get; init; }
    public required string CountHeader { get; init; }
    public required string DetailsTitle { get; init; }
    public required string NoResultsText { get; init; }
    public required string ErrorText { get; init; }
    public required string WarningText { get; init; }
    public required string MessageText { get; init; }
}
