namespace Revit.Linter.DiagnosticReportExporting.Abstractions.Models;

/// <summary>
/// The options the save-file dialog is shown with.
/// </summary>
/// <param name="FileName">The suggested file name.</param>
/// <param name="DefaultExtension">The default extension of the first exporter.</param>
/// <param name="Filters">The file-type filters, one per registered exporter, in registration order.</param>
public sealed record SaveFileDialogOptions(
    string FileName,
    string DefaultExtension,
    IReadOnlyList<string> Filters);

/// <summary>
/// The outcome of the save-file dialog.
/// </summary>
/// <param name="Confirmed">Whether the user confirmed the dialog.</param>
/// <param name="FileName">The file name chosen by the user.</param>
/// <param name="FilterIndex">The one-based index of the chosen filter.</param>
public sealed record SaveFileDialogResult(
    bool Confirmed,
    string FileName,
    int FilterIndex);
