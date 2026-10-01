using Microsoft.Extensions.Logging;
using Revit.Linter.Updater.Core.Abstractions;
using Revit.Linter.Updater.Core.Models;
using System.Text.Json;

namespace Revit.Linter.Updater.Core.Services;

/// <summary>Applies updater settings, scheduling, version comparison, and skip behavior.</summary>
public sealed class UpdateCoordinator
{
    /// <summary>The built-in interval between successful automatic checks.</summary>
    public static readonly TimeSpan AutomaticCheckInterval = UpdaterConfiguration.DefaultAutomaticCheckInterval;

    private readonly IGitHubReleaseClient _releaseClient;
    private readonly IUpdaterStateStore _stateStore;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<UpdateCoordinator> _logger;
    private readonly UpdaterConfiguration _configuration;

    /// <summary>Creates an update-check coordinator.</summary>
    public UpdateCoordinator(
        IGitHubReleaseClient releaseClient,
        IUpdaterStateStore stateStore,
        UpdaterConfiguration configuration,
        TimeProvider timeProvider,
        ILogger<UpdateCoordinator> logger)
    {
        _releaseClient = releaseClient;
        _stateStore = stateStore;
        _configuration = configuration;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>Checks for a newer stable release according to the selected mode.</summary>
    /// <remarks>An unsuccessful request never advances <see cref="UpdaterState.LastCheckedAt"/>.</remarks>
    public async Task<UpdateCheckResult> CheckAsync(
        StableVersion currentVersion,
        UpdateCheckMode mode,
        CancellationToken cancellationToken = default)
    {
        if (!_configuration.ChecksEnabled)
            return new UpdateCheckResult(UpdateCheckStatus.Disabled, currentVersion);

        UpdaterState state;
        try
        {
            state = await _stateStore.LoadAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogError(exception, "Failed to load updater state");
            return new UpdateCheckResult(UpdateCheckStatus.Failed, currentVersion, Error: exception.Message);
        }

        if (mode == UpdateCheckMode.Automatic)
        {
            if (!_configuration.AutomaticChecksEnabled)
                return new UpdateCheckResult(UpdateCheckStatus.Disabled, currentVersion);
            if (state.LastCheckedAt is { } lastChecked &&
                _timeProvider.GetUtcNow() - lastChecked < _configuration.AutomaticCheckInterval)
            {
                return new UpdateCheckResult(UpdateCheckStatus.NotDue, currentVersion);
            }
        }

        try
        {
            ReleaseInfo? release = await _releaseClient.GetLatestAsync(cancellationToken);
            state.LastCheckedAt = _timeProvider.GetUtcNow();
            await _stateStore.SaveAsync(state, cancellationToken);

            if (release is null || release.Version.CompareTo(currentVersion) <= 0)
                return new UpdateCheckResult(UpdateCheckStatus.UpToDate, currentVersion, release);
            if (StableVersion.TryParse(state.SkippedVersion, out StableVersion skipped) && skipped == release.Version)
                return new UpdateCheckResult(UpdateCheckStatus.Skipped, currentVersion, release);

            return new UpdateCheckResult(UpdateCheckStatus.UpdateAvailable, currentVersion, release);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or JsonException or IOException or UnauthorizedAccessException ||
            exception is TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Update check failed");
            return new UpdateCheckResult(UpdateCheckStatus.Failed, currentVersion, Error: exception.Message);
        }
    }
}
