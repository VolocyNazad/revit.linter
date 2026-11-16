using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using System.ComponentModel;
using System.IO;
using System.Text.Json;

namespace Revit.Linter.Infrastructure.Services;

/// <summary>
/// Shows a Revit update prompt once per start when the separate updater finds a newer stable release.
/// </summary>
/// <remarks>
/// The add-in never contacts GitHub. It starts the updater in a one-shot startup mode, waits for the
/// temporary result file and shows the three-button dialog on the Revit UI thread. A failed or slow
/// check is logged and never affects add-in startup. The dialog's skip decision is persisted by the
/// updater, so it also suppresses the Windows notification for the same version.
/// </remarks>
internal sealed class StartupUpdateNotificationService(
    IRevitIdlingScheduler idlingScheduler,
    IUpdateAvailableDialog updateDialog,
    IShellLauncher shellLauncher,
    ILogger<StartupUpdateNotificationService> logger)
{
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan SkipTimeout = TimeSpan.FromSeconds(15);

    /// <summary>Queues the background update check for the next Revit Idling event.</summary>
    public void Schedule()
    {
        _ = idlingScheduler.RunAsync(application =>
        {
            _ = CheckAndNotifyAsync();
        });
    }

    private async Task CheckAndNotifyAsync()
    {
        RevitUpdateNotification? notification;
        try
        {
            notification = await CheckAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to check for a Revit Linter update at startup");
            return;
        }

        if (notification is null)
            return;

        // The dialog must be created on the Revit UI thread, so its display is queued to Idling.
        _ = idlingScheduler.RunAsync(_ => ShowDialog(notification));
    }

    private async Task<RevitUpdateNotification?> CheckAsync()
    {
        string resultPath = Path.Combine(
            UpdaterDirectory,
            $"revit-startup-{Guid.NewGuid():N}.json");
        try
        {
            Directory.CreateDirectory(UpdaterDirectory);
            string arguments = $"--check-for-revit \"{resultPath}\"";
            bool completed = await UpdaterProcessRunner
                .RunAndWaitAsync(arguments, CheckTimeout)
                .ConfigureAwait(false);
            if (!completed || !File.Exists(resultPath))
                return null;

            using FileStream stream = File.OpenRead(resultPath);
            RevitUpdateNotification? notification =
                await JsonSerializer.DeserializeAsync<RevitUpdateNotification>(stream).ConfigureAwait(false);
            if (notification is null ||
                !HasStableVersionText(notification.Version) ||
                !notification.HasTrustedReleasePage())
            {
                logger.LogWarning("Ignored an invalid startup update result from the updater");
                return null;
            }

            return notification;
        }
        catch (FileNotFoundException exception)
        {
            logger.LogInformation(
                exception,
                "The Revit Linter updater is not installed; the startup check is skipped");
            return null;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or
            Win32Exception or InvalidOperationException)
        {
            logger.LogError(exception, "The Revit Linter updater could not perform the startup check");
            return null;
        }
        finally
        {
            TryDelete(resultPath);
        }
    }

    private void ShowDialog(RevitUpdateNotification notification)
    {
        try
        {
            UpdateAvailableDialogChoice choice = updateDialog
                .Show(new UpdateAvailableDialogRequest(notification.Version, notification.ReleasePage))
                .GetAwaiter()
                .GetResult();
            switch (choice)
            {
                case UpdateAvailableDialogChoice.SkipVersion:
                    _ = SkipVersionAsync(notification.Version);
                    break;
                case UpdateAvailableDialogChoice.OpenRelease:
                    _ = shellLauncher.OpenUrl(notification.ReleasePage);
                    break;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to show the Revit Linter update prompt");
        }
    }

    private async Task SkipVersionAsync(string version)
    {
        try
        {
            bool completed = await UpdaterProcessRunner
                .RunAndWaitAsync($"--skip-version {version}", SkipTimeout)
                .ConfigureAwait(false);
            if (!completed)
                logger.LogWarning("Skipping update {Version} did not finish in time", version);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to persist skipped update {Version}", version);
        }
    }

    private static string UpdaterDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ProductIdentity.CompanyDirectoryName,
        ProductIdentity.ProductDirectoryName,
        ProductIdentity.UpdaterFolderName);

    private static bool HasStableVersionText(string? value)
    {
        return Version.TryParse(value, out Version? version) &&
            version.Build >= 0 &&
            version.Revision < 0;
    }

    private void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(exception, "Failed to remove the startup update result {Path}", path);
        }
    }
}
