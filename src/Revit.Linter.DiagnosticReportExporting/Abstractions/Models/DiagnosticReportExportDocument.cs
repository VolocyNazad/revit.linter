namespace Revit.Linter.DiagnosticReportExporting.Abstractions.Models;

/// <summary>
/// The versioned export document written by every report exporter.
/// </summary>
/// <param name="SchemaVersion">The document schema version.</param>
/// <param name="Metadata">The export metadata.</param>
/// <param name="Items">The exported findings.</param>
public sealed record DiagnosticReportExportDocument(
    string SchemaVersion,
    DiagnosticReportExportMetadata Metadata,
    IReadOnlyCollection<DiagnosticReportExportItem> Items);

/// <summary>
/// Describes when and from what the report was exported.
/// </summary>
/// <param name="ExportedAt">The moment the report was exported.</param>
/// <param name="RevitVersion">The running Revit version number.</param>
/// <param name="RevitBuild">The running Revit build number.</param>
/// <param name="AddInVersion">The add-in version the report was exported from.</param>
/// <param name="DocumentTitle">The exported document title, or the all-documents text.</param>
/// <param name="Scope">The export scope (<c>AllDocuments</c> or <c>Document</c>).</param>
/// <param name="Filters">The filters applied to the exported rows.</param>
/// <param name="TotalItemCount">The number of rows before filtering.</param>
/// <param name="ExportedItemCount">The number of exported rows.</param>
public sealed record DiagnosticReportExportMetadata(
    DateTimeOffset ExportedAt,
    string RevitVersion,
    string RevitBuild,
    string AddInVersion,
    string DocumentTitle,
    string Scope,
    DiagnosticReportExportFilters Filters,
    int TotalItemCount,
    int ExportedItemCount);

/// <summary>
/// The filters applied to the exported rows.
/// </summary>
/// <param name="SearchText">The search text, or <see langword="null"/> when the search box is empty.</param>
/// <param name="Severities">The active severity names.</param>
/// <param name="States">The active row states.</param>
public sealed record DiagnosticReportExportFilters(
    string? SearchText,
    IReadOnlyCollection<string> Severities,
    IReadOnlyCollection<string> States);
