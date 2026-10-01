using Revit.Linter.Updater.Core.Models;

namespace Revit.Linter.Updater.Core.Abstractions;

/// <summary>Retrieves the latest stable Revit Linter release.</summary>
public interface IGitHubReleaseClient
{
    /// <summary>Returns the latest stable release, or <see langword="null"/> when none exists.</summary>
    Task<ReleaseInfo?> GetLatestAsync(CancellationToken cancellationToken = default);
}
