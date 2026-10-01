using Microsoft.Extensions.Logging;
using Revit.Linter.Updater.Core.Abstractions;
using Revit.Linter.Updater.Core.Models;
using System.Text.Json;

namespace Revit.Linter.Updater.Core.Services;

/// <summary>Applies persisted notification decisions to update-check results.</summary>
public sealed class UpdateNotificationCoordinator
{
    private readonly IUpdateNotificationService _notificationService;
    private readonly IUpdaterStateStore _stateStore;
    private readonly ILogger<UpdateNotificationCoordinator> _logger;
    private readonly UpdaterConfiguration _configuration;

    /// <summary>Creates a coordinator for notification decisions and persisted state.</summary>
    public UpdateNotificationCoordinator(
        IUpdateNotificationService notificationService,
        IUpdaterStateStore stateStore,
        UpdaterConfiguration configuration,
        ILogger<UpdateNotificationCoordinator> logger)
    {
        _notificationService = notificationService;
        _stateStore = stateStore;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Shows a notification when the result contains a newer release not notified before.</summary>
    /// <remarks>
    /// Notification and state failures are logged and converted to <see langword="false"/> so update
    /// discovery and Revit startup remain unaffected. State is advanced only after successful submission.
    /// </remarks>
    public async Task<bool> NotifyIfNeededAsync(
        UpdateCheckResult result,
        CancellationToken cancellationToken = default)
    {
        if (!_configuration.NotificationsEnabled ||
            result.Status != UpdateCheckStatus.UpdateAvailable ||
            result.Release is null)
            return false;

        try
        {
            UpdaterState state = await _stateStore.LoadAsync(cancellationToken);
            string version = result.Release.Version.ToString();
            if (string.Equals(state.LastNotifiedVersion, version, StringComparison.Ordinal))
                return false;

            if (!await _notificationService.TryShowAsync(result.Release, cancellationToken))
                return false;

            state.LastNotifiedVersion = version;
            await _stateStore.SaveAsync(state, cancellationToken);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogError(exception, "Failed to update notification state");
            return false;
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Update notification is unavailable");
            return false;
        }
    }
}
