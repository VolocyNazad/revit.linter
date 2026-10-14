using Microsoft.Win32;
using Revit.Linter.DiagnosticReportExporting.Abstractions.Models;
using Revit.Linter.DiagnosticReportExporting.Abstractions.Services;

namespace Revit.Linter.DiagnosticReportExporting.Infrastructure.Services;

internal sealed class SaveFileDialogService : ISaveFileDialogService
{
    public SaveFileDialogResult ShowSaveDialog(SaveFileDialogOptions options)
    {
        SaveFileDialog dialog = new()
        {
            AddExtension = true,
            DefaultExt = options.DefaultExtension,
            FileName = options.FileName,
            Filter = string.Join("|", options.Filters),
            FilterIndex = 1,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true)
            return new SaveFileDialogResult(false, string.Empty, dialog.FilterIndex);

        return new SaveFileDialogResult(true, dialog.FileName, dialog.FilterIndex);
    }
}
