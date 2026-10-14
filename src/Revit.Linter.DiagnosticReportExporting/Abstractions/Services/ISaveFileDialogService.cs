using Revit.Linter.DiagnosticReportExporting.Abstractions.Models;

namespace Revit.Linter.DiagnosticReportExporting.Abstractions.Services;

/// <summary>
/// Shows the save-file dialog the report export starts from.
/// </summary>
public interface ISaveFileDialogService
{
    /// <summary>
    /// Shows a modal save-file dialog with the specified options.
    /// </summary>
    /// <param name="options">The suggested file name, default extension and filters.</param>
    /// <returns>The dialog outcome, including the chosen file name and filter.</returns>
    /// <remarks>
    /// The call shows a modal dialog and must run on the UI thread.
    /// </remarks>
    SaveFileDialogResult ShowSaveDialog(SaveFileDialogOptions options);
}
