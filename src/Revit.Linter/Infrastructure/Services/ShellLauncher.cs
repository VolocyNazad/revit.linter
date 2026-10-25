using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Revit.Context.Abstractions.Services;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.Infrastructure.Utils;
using System.Diagnostics;
using System.IO;

namespace Revit.Linter.Infrastructure.Services;

/// <summary>
/// Hands configuration files, folders and addresses to the Windows shell for the presenter modules.
/// </summary>
/// <remarks>
/// This is the single place where the add-in starts shell processes. A failed launch is logged once and
/// shown to the user, and the method returns <see langword="false"/> instead of throwing.
/// </remarks>
internal sealed class ShellLauncher(
    IRevitContext revitContext,
    IDialog dialog,
    IStringLocalizer<GlobalLocalizations> localizer,
    ILogger<ShellLauncher> logger) : IShellLauncher
{
    /// <inheritdoc />
    public bool Open(string path) =>
        Start(new ProcessStartInfo(path) { UseShellExecute = true }, path, "configuration_openFailed_message");

    /// <inheritdoc />
    public bool OpenFolder(string path) =>
        Start(new ProcessStartInfo(path) { UseShellExecute = true }, path, "configurationFolder_openFailed_message");

    /// <inheritdoc />
    public bool OpenWith(string path)
    {
        try
        {
            return OpenWithDialog.Show(revitContext.UIApplication?.MainWindowHandle ?? IntPtr.Zero, path);
        }
        catch (Exception exception)
        {
            Report(exception, path, "configuration_openFailed_message");
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
            path,
            "configuration_openFailed_message");

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
            path,
            "configuration_openFailed_message");

    /// <inheritdoc />
    public bool OpenUrl(string url) =>
        Start(new ProcessStartInfo(url) { UseShellExecute = true }, url, "externalPage_openFailed_message");

    private bool Start(ProcessStartInfo startInfo, string target, string failureMessageKey)
    {
        try
        {
            Process.Start(startInfo);
            return true;
        }
        catch (Exception exception)
        {
            Report(exception, target, failureMessageKey);
            return false;
        }
    }

    private void Report(Exception exception, string target, string failureMessageKey)
    {
        logger.LogError(exception, "Failed to open {ShellTarget}", target);
        _ = dialog.Show(new DialogRequest(localizer[failureMessageKey, exception.Message]));
    }
}
