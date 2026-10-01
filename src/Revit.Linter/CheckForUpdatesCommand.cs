using Autodesk.Revit.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.Infrastructure.ExternalCommands;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Revit.Linter;

/// <summary>Starts the standalone Revit Linter updater for an explicit user check.</summary>
/// <remarks>
/// The add-in performs no network request. The updater executable owns release discovery and user
/// feedback, so a failed update check cannot affect Revit startup or the active document.
/// </remarks>
[Transaction(TransactionMode.Manual)]
public sealed class CheckForUpdatesCommand : ExternalCommand
{
    private const string Vendor = "VolocyNazad";
    private const string UpdaterExecutableName = "Revit.Linter.Updater.exe";

    /// <inheritdoc />
    public override void Execute()
    {
        try
        {
            string updaterPath = ResolveUpdaterPath();
            Process.Start(new ProcessStartInfo
            {
                FileName = updaterPath,
                Arguments = "--check-now",
                WorkingDirectory = Path.GetDirectoryName(updaterPath),
                UseShellExecute = true
            });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or Win32Exception)
        {
            IServiceProvider provider = Program.Provider;
            provider.GetRequiredService<ILogger<CheckForUpdatesCommand>>()
                .LogError(exception, "Failed to start the standalone updater");
            var localizer = provider.GetRequiredService<IStringLocalizer<GlobalLocalizations>>();
            _ = provider.GetRequiredService<IDialog>().Show(new DialogRequest(
                localizer["updater_startFailed_message", exception.Message]));
        }
    }

    private static string ResolveUpdaterPath()
    {
        string installedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", Vendor, "Revit.Linter", UpdaterExecutableName);
        if (File.Exists(installedPath))
            return installedPath;

        string? assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string adjacentPath = Path.Combine(assemblyDirectory ?? string.Empty, UpdaterExecutableName);
        if (File.Exists(adjacentPath))
            return adjacentPath;

        throw new FileNotFoundException("The Revit Linter updater executable was not found.", installedPath);
    }
}
