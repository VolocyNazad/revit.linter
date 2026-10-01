using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Revit.Linter.Updater.Core.Abstractions;
using Revit.Linter.Updater.Core.Models;
using Revit.Linter.Updater.Core.Services;

namespace Revit.Linter.Updater;

internal sealed class WindowsUpdateNotificationService : IUpdateNotificationService, IDisposable
{
    private readonly AppNotificationManager? _manager;
    private readonly UpdateNotificationActivationHandler _activationHandler;
    private readonly InstallerDownloadService _installerDownloader;
    private readonly ILogger<WindowsUpdateNotificationService> _logger;

    public WindowsUpdateNotificationService(
        UpdateNotificationActivationHandler activationHandler,
        InstallerDownloadService installerDownloader,
        ILogger<WindowsUpdateNotificationService> logger)
    {
        _activationHandler = activationHandler;
        _installerDownloader = installerDownloader;
        _logger = logger;

        if (IsElevated())
        {
            _logger.LogWarning("Windows update notifications are unavailable in an elevated process");
            return;
        }

        try
        {
            if (!AppNotificationManager.IsSupported())
            {
                _logger.LogWarning("Windows app notifications are not supported on this system");
                return;
            }

            AppNotificationManager manager = AppNotificationManager.Default;
            manager.NotificationInvoked += OnNotificationInvoked;
            try
            {
                manager.Register();
                _manager = manager;
            }
            catch
            {
                manager.NotificationInvoked -= OnNotificationInvoked;
                throw;
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Windows app notification registration failed");
        }
    }

    public Task<bool> TryShowAsync(ReleaseInfo release, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_manager is null)
            return Task.FromResult(false);

        try
        {
            if (_manager.Setting != AppNotificationSetting.Enabled)
            {
                _logger.LogWarning(
                    "Windows app notifications are disabled with setting {NotificationSetting}",
                    _manager.Setting);
                return Task.FromResult(false);
            }

            string version = release.Version.ToString();
            var notification = new AppNotificationBuilder()
                .AddArgument("action", "release")
                .AddArgument("version", version)
                .AddArgument("url", release.ReleasePage.AbsoluteUri)
                .AddText($"Revit Linter {version} is available")
                .AddText("Download the update or review the release notes.")
                .AddButton(CreateDownloadButton(release))
                .AddButton(CreateButton("What's new", "release", version, release.ReleasePage))
                .AddButton(CreateButton("Later", "later", version, release.ReleasePage))
                .AddButton(CreateButton("Skip this version", "skip", version, release.ReleasePage))
                .BuildNotification();

            _manager.Show(notification);
            _logger.LogInformation("Displayed update notification for version {AvailableVersion}", version);
            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to display Windows update notification");
            return Task.FromResult(false);
        }
    }

    public void Dispose()
    {
        if (_manager is null)
            return;

        try
        {
            _manager.NotificationInvoked -= OnNotificationInvoked;
            _manager.Unregister();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Windows app notification shutdown failed");
        }
    }

    private static AppNotificationButton CreateButton(
        string label,
        string action,
        string version,
        Uri releasePage) =>
        new AppNotificationButton(label)
            .AddArgument("action", action)
            .AddArgument("version", version)
            .AddArgument("url", releasePage.AbsoluteUri);

    private static AppNotificationButton CreateDownloadButton(ReleaseInfo release)
    {
        if (release.Installer is not { } installer)
            return CreateButton("Download", "release", release.Version.ToString(), release.ReleasePage);

        return CreateButton("Download", "download", release.Version.ToString(), release.ReleasePage)
            .AddArgument("name", installer.Name)
            .AddArgument("downloadUrl", installer.DownloadUri.AbsoluteUri)
            .AddArgument("size", installer.Size.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .AddArgument("digest", $"sha256:{installer.Sha256}");
    }

    private void OnNotificationInvoked(
        AppNotificationManager sender,
        AppNotificationActivatedEventArgs arguments)
    {
        try
        {
            HandleActivationAsync(arguments.Argument).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to handle update notification action");
        }
    }

    private async Task HandleActivationAsync(string arguments)
    {
        if (!UpdateNotificationActivation.TryParse(arguments, out UpdateNotificationActivation? activation))
        {
            _logger.LogWarning("Ignored invalid update notification activation arguments");
            return;
        }

        if (activation.Action == UpdateNotificationAction.Download)
        {
            await DownloadInstallerAsync(activation);
            return;
        }

        Uri? releasePage = await _activationHandler.HandleAsync(activation);
        if (releasePage is not null)
            Process.Start(new ProcessStartInfo(releasePage.AbsoluteUri) { UseShellExecute = true });
    }

    private async Task DownloadInstallerAsync(UpdateNotificationActivation activation)
    {
        using var operationTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(30));
        try
        {
            string downloadDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Volocy", "Revit.Linter", "updater", "downloads");
            string installerPath = await _installerDownloader.DownloadAsync(
                activation.Installer!,
                downloadDirectory,
                operationTimeout.Token);
            string explorerPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "explorer.exe");
            Process.Start(new ProcessStartInfo
            {
                FileName = explorerPath,
                Arguments = $"/select,\"{installerPath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or IOException or UnauthorizedAccessException or TaskCanceledException)
        {
            _logger.LogError(exception, "Failed to download or verify installer");
            Process.Start(new ProcessStartInfo(activation.ReleasePage!.AbsoluteUri) { UseShellExecute = true });
        }
    }

    private static bool IsElevated()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
