using Revit.Linter.DiagnosticReportExporting.Abstractions.Models;

namespace Revit.Linter.DiagnosticReportExporting.Abstractions.Services;

/// <summary>
/// Writes a diagnostic report document to a file.
/// </summary>
public interface IDiagnosticReportExporter
{
    /// <summary>Gets the file extension this exporter writes, including the leading dot.</summary>
    string Extension { get; }

    /// <summary>Gets the save-dialog filter for this exporter (for example <c>CSV (*.csv)|*.csv</c>).</summary>
    string Filter { get; }

    /// <summary>
    /// Writes the report document to the specified file.
    /// </summary>
    /// <param name="fileName">The file path to write.</param>
    /// <param name="context">The cultures and localized texts the report is rendered with.</param>
    /// <param name="document">The versioned report document.</param>
    /// <remarks>
    /// The call writes to the file system; the caller picks the file name. JSON and YAML are a versioned
    /// contract for external tools, so their shape must only change deliberately.
    /// </remarks>
    void Export(
        string fileName,
        DiagnosticReportExportContext context,
        DiagnosticReportExportDocument document);
}
