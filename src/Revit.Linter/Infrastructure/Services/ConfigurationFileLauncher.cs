using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Revit.Context.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.DiagnosticListPresenter.Abstractions.Services;
using Revit.Linter.Infrastructure.Utils;
using System.Diagnostics;
using System.IO;

namespace Revit.Linter.Infrastructure.Services;

/// <summary>Opens diagnostic configuration files with the shell, reporting a failed launch to the user.</summary>
internal sealed class ConfigurationFileLauncher(
    IRevitContext revitContext,
    IDialog dialog,
    IStringLocalizer<GlobalLocalizations> localizer,
    ILogger<ConfigurationFileLauncher> logger) : IConfigurationFileLauncher
{
    /// <inheritdoc />
    public bool Open(string path) =>
        Start(new ProcessStartInfo(path) { UseShellExecute = true }, path);

    /// <inheritdoc />
    public bool OpenWith(string path)
    {
        try
        {
            return OpenWithDialog.Show(revitContext.UIApplication?.MainWindowHandle ?? IntPtr.Zero, path);
        }
        catch (Exception exception)
        {
            Report(exception, path);
            return false;
        }
    }

    /// <inheritdoc />
    public bool OpenInNotepad(string path) =>
        Start(
            new ProcessStartInfo(
                Path.Combine(Environment.SystemDirectory, "notepad.exe"),
                $"\"{path}\"")
            {
                UseShellExecute = true,
            },
            path);

    /// <inheritdoc />
    public bool RevealInExplorer(string path) =>
        Start(
            new ProcessStartInfo(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    "explorer.exe"),
                $"/select,\"{path}\"")
            {
                UseShellExecute = true,
            },
            path);

    private bool Start(ProcessStartInfo startInfo, string path)
    {
        try
        {
            Process.Start(startInfo);
            return true;
        }
        catch (Exception exception)
        {
            Report(exception, path);
            return false;
        }
    }

    private void Report(Exception exception, string path)
    {
        logger.LogError(exception, "Failed to open configuration file {ConfigurationPath}", path);
        _ = dialog.Show(new DialogRequest(localizer["configuration_openFailed_message", exception.Message]));
    }
}
