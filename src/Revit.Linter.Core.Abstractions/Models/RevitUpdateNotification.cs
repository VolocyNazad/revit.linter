namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>
/// Describes a newer stable release that the separate updater found for the Revit add-in.
/// </summary>
/// <remarks>
/// The add-in receives this notification from the updater as a one-shot startup result. It never
/// performs a release request itself; the value only carries what the dialog needs to show.
/// </remarks>
/// <param name="Version">The stable version of the available release.</param>
/// <param name="ReleasePage">The absolute address of the GitHub release page.</param>
public sealed record RevitUpdateNotification(string Version, string ReleasePage)
{
    /// <summary>
    /// Returns whether the release address points to a release page of the official repository.
    /// </summary>
    /// <returns><see langword="true"/> when the address is a trusted HTTPS release page.</returns>
    public bool HasTrustedReleasePage()
    {
        return Uri.TryCreate(ReleasePage, UriKind.Absolute, out Uri? releasePage) &&
            releasePage.Scheme == Uri.UriSchemeHttps &&
            string.Equals(releasePage.Host, ProductIdentity.GitHubHost, StringComparison.OrdinalIgnoreCase) &&
            releasePage.IsDefaultPort &&
            string.IsNullOrEmpty(releasePage.UserInfo) &&
            releasePage.AbsolutePath.StartsWith(
                ProductIdentity.RepositoryReleasePath,
                StringComparison.Ordinal);
    }
}
