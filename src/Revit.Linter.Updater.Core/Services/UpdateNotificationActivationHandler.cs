using Microsoft.Extensions.Logging;
using Revit.Linter.Updater.Core.Abstractions;
using Revit.Linter.Updater.Core.Models;
using System.Text.Json;

namespace Revit.Linter.Updater.Core.Services;

/// <summary>Applies a validated update-notification action to updater state.</summary>
public sealed class UpdateNotificationActivationHandler
{
    private readonly IUpdaterStateStore _stateStore;
    private readonly ILogger<UpdateNotificationActivationHandler> _logger;

    /// <summary>Creates a handler for persisted notification actions.</summary>
    public UpdateNotificationActivationHandler(
        IUpdaterStateStore stateStore,
        ILogger<UpdateNotificationActivationHandler> logger)
    {
        _stateStore = stateStore;
        _logger = logger;
    }

    /// <summary>Applies an action and returns the trusted page that the platform adapter should open.</summary>
    /// <remarks>
    /// <see cref="UpdateNotificationAction.Later"/> clears the matching notification marker so the
    /// release may be offered after the next scheduled check. Skip persists the selected stable version.
    /// Persistence failures are logged and ignored so notification activation cannot break the updater.
    /// </remarks>
    public async Task<Uri?> HandleAsync(
        UpdateNotificationActivation activation,
        CancellationToken cancellationToken = default)
    {
        if (activation.Action is UpdateNotificationAction.Download or UpdateNotificationAction.ReleaseNotes)
            return activation.ReleasePage;

        try
        {
            UpdaterState state = await _stateStore.LoadAsync(cancellationToken);
            switch (activation.Action)
            {
                case UpdateNotificationAction.SkipVersion:
                    state.SkippedVersion = activation.Version.ToString();
                    await _stateStore.SaveAsync(state, cancellationToken);
                    _logger.LogInformation("Skipped update version {SkippedVersion}", activation.Version);
                    break;
                case UpdateNotificationAction.Later:
                    if (string.Equals(
                            state.LastNotifiedVersion,
                            activation.Version.ToString(),
                            StringComparison.Ordinal))
                    {
                        state.LastNotifiedVersion = null;
                        await _stateStore.SaveAsync(state, cancellationToken);
                    }
                    _logger.LogInformation("Deferred update version {DeferredVersion}", activation.Version);
                    break;
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogError(exception, "Failed to persist update notification action");
        }

        return null;
    }
}
