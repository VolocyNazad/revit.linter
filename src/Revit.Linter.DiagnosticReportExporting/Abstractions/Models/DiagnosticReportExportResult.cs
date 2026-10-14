namespace Revit.Linter.DiagnosticReportExporting.Abstractions.Models;

/// <summary>
/// The outcome of an export orchestration run.
/// </summary>
/// <param name="Completed">
/// Whether a report file was written; <see langword="false"/> when the user cancels the dialog
/// or no exporter is registered.
/// </param>
/// <param name="FilePath">The written file path, or <see langword="null"/> when nothing was written.</param>
/// <param name="Extension">The extension of the used exporter (for example <c>.csv</c>), or <see langword="null"/>.</param>
public sealed record DiagnosticReportExportResult(
    bool Completed,
    string? FilePath,
    string? Extension);
