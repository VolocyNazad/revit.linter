using Microsoft.Extensions.Logging;
using Revit.Linter.Updater.Core.Abstractions;
using Revit.Linter.Updater.Core.Models;
using System.Text.Json;

namespace Revit.Linter.Updater.Core.Services;

/// <summary>Persists a stable version that the user chose not to be offered again.</summary>
public sealed class UpdateSkipService
{
    private readonly IUpdaterStateStore _stateStore;
    private readonly ILogger<UpdateSkipService> _logger;

    /// <summary>Creates a service that records skipped stable versions in the user updater state.</summary>
    public UpdateSkipService(IUpdaterStateStore stateStore, ILogger<UpdateSkipService> logger)
    {
        _stateStore = stateStore;
        _logger = logger;
    }

    /// <summary>Stores a stable version as skipped until a newer stable version is published.</summary>
    /// <param name="versionText">The stable version text, for example <c>3.2.0</c>.</param>
    /// <param name="cancellationToken">A token that cancels the state operation.</param>
    /// <returns><see langword="true"/> when the version was persisted; otherwise <see langword="false"/>.</returns>
    public async Task<bool> SkipAsync(string? versionText, CancellationToken cancellationToken = default)
    {
        if (!StableVersion.TryParse(versionText, out StableVersion version))
        {
            _logger.LogWarning("Ignored an invalid skipped version {Version}", versionText);
            return false;
        }

        try
        {
            UpdaterState state = await _stateStore.LoadAsync(cancellationToken);
            state.SkippedVersion = version.ToString();
            await _stateStore.SaveAsync(state, cancellationToken);
            _logger.LogInformation("Update {Version} will not be offered again", version);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogError(exception, "Failed to persist skipped version {Version}", version);
            return false;
        }
    }
}
