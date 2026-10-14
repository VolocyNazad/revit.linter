using Revit.Linter.DiagnosticReportExporting.Abstractions.Models;

namespace Revit.Linter.DiagnosticReportExporting.Abstractions.Services;

/// <summary>
/// Exports the displayed diagnostic findings to a file.
/// </summary>
public interface IDiagnosticReportExportService
{
    /// <summary>
    /// Asks the user for a file and writes the requested findings with the matching exporter.
    /// </summary>
    /// <param name="request">The findings, filters, labels and environment versions to export.</param>
    /// <returns>
    /// The export outcome; <see cref="DiagnosticReportExportResult.Completed"/> is <see langword="false"/>
    /// when the user cancels the dialog or no exporter is registered.
    /// </returns>
    /// <remarks>
    /// The call shows a modal save-file dialog, so it must run on the UI thread, and writes the chosen
    /// file through the exporter matching the dialog filter. It publishes no user-interface activities;
    /// the caller reports the completed export with the returned extension.
    /// </remarks>
    DiagnosticReportExportResult Export(DiagnosticReportExportRequest request);
}
