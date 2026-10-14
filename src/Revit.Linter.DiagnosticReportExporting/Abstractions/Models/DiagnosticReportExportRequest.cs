namespace Revit.Linter.DiagnosticReportExporting.Abstractions.Models;

/// <summary>
/// Everything the export orchestration needs to write a report file.
/// </summary>
/// <param name="Items">The filtered findings to export.</param>
/// <param name="TotalItemCount">The number of rows before filtering.</param>
/// <param name="TargetDocumentTitle">The exported document title, or <see langword="null"/> for every document.</param>
/// <param name="SearchText">The search text, or <see langword="null"/> when the search box is empty.</param>
/// <param name="Severities">The active severity names.</param>
/// <param name="States">The active row states.</param>
/// <param name="Labels">The cultures and localized texts the report is rendered with.</param>
/// <param name="RevitVersion">The running Revit version number.</param>
/// <param name="RevitBuild">The running Revit build number.</param>
/// <param name="AddInVersion">The add-in version the report is exported from.</param>
/// <param name="ExportedAt">The moment recorded in the metadata and the suggested file name.</param>
public sealed record DiagnosticReportExportRequest(
    IReadOnlyList<DiagnosticReportExportItem> Items,
    int TotalItemCount,
    string? TargetDocumentTitle,
    string? SearchText,
    IReadOnlyList<string> Severities,
    IReadOnlyList<string> States,
    DiagnosticReportExportLabels Labels,
    string RevitVersion,
    string RevitBuild,
    string AddInVersion,
    DateTimeOffset ExportedAt);
