namespace Revit.Linter.DiagnosticReportPresenter.Exporting;

internal sealed record DiagnosticReportExportDocument(
    string SchemaVersion,
    DiagnosticReportExportMetadata Metadata,
    IReadOnlyCollection<DiagnosticReportExportItem> Items);

internal sealed record DiagnosticReportExportMetadata(
    DateTimeOffset ExportedAt,
    string RevitVersion,
    string RevitBuild,
    string AddInVersion,
    string DocumentTitle,
    string Scope,
    DiagnosticReportExportFilters Filters,
    int TotalItemCount,
    int ExportedItemCount);

internal sealed record DiagnosticReportExportFilters(
    string? SearchText,
    IReadOnlyCollection<string> Severities,
    IReadOnlyCollection<string> States);
