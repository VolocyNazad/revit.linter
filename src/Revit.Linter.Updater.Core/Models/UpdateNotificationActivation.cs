using System.Diagnostics.CodeAnalysis;
using System.Web;

namespace Revit.Linter.Updater.Core.Models;

/// <summary>Represents validated arguments received from a Windows update notification.</summary>
public sealed class UpdateNotificationActivation
{
    private const string RepositoryReleasePath = "/VolocyNazad/revit.linter/releases/";

    private UpdateNotificationActivation(
        UpdateNotificationAction action,
        StableVersion version,
        Uri? releasePage)
    {
        Action = action;
        Version = version;
        ReleasePage = releasePage;
    }

    /// <summary>Gets the validated action selected by the user.</summary>
    public UpdateNotificationAction Action { get; }

    /// <summary>Gets the stable release version embedded in the notification.</summary>
    public StableVersion Version { get; }

    /// <summary>Gets the trusted official release page for a navigation action.</summary>
    /// <remarks>This is <see langword="null"/> for actions that do not navigate externally.</remarks>
    public Uri? ReleasePage { get; }

    /// <summary>Parses and validates the arguments supplied by notification activation.</summary>
    /// <remarks>
    /// External navigation is accepted only for HTTPS release pages in the official GitHub repository.
    /// Unknown actions, unstable versions, user info, non-default ports, fragments, and other origins
    /// are rejected. The <see cref="UpdateNotificationAction.Later"/> and
    /// <see cref="UpdateNotificationAction.SkipVersion"/> actions do not consume the supplied URL.
    /// </remarks>
    public static bool TryParse(
        string? arguments,
        [NotNullWhen(true)] out UpdateNotificationActivation? activation)
    {
        activation = null;
        if (string.IsNullOrWhiteSpace(arguments))
            return false;

        var values = HttpUtility.ParseQueryString(arguments);
        if (!StableVersion.TryParse(values["version"], out StableVersion version) ||
            !TryParseAction(values["action"], out UpdateNotificationAction action))
        {
            return false;
        }

        Uri? releasePage = null;
        if (action is UpdateNotificationAction.Download or UpdateNotificationAction.ReleaseNotes &&
            !TryGetTrustedReleaseUri(values["url"], out releasePage))
        {
            return false;
        }

        activation = new UpdateNotificationActivation(action, version, releasePage);
        return true;
    }

    private static bool TryParseAction(string? value, out UpdateNotificationAction action)
    {
        action = value switch
        {
            "download" => UpdateNotificationAction.Download,
            "release" => UpdateNotificationAction.ReleaseNotes,
            "later" => UpdateNotificationAction.Later,
            "skip" => UpdateNotificationAction.SkipVersion,
            _ => default
        };
        return value is "download" or "release" or "later" or "skip";
    }

    private static bool TryGetTrustedReleaseUri(
        string? value,
        [NotNullWhen(true)] out Uri? releasePage)
    {
        bool trusted = Uri.TryCreate(value, UriKind.Absolute, out releasePage) &&
                       releasePage.Scheme == Uri.UriSchemeHttps &&
                       releasePage.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) &&
                       releasePage.IsDefaultPort &&
                       string.IsNullOrEmpty(releasePage.UserInfo) &&
                       string.IsNullOrEmpty(releasePage.Fragment) &&
                       releasePage.AbsolutePath.StartsWith(RepositoryReleasePath, StringComparison.OrdinalIgnoreCase);
        if (!trusted)
            releasePage = null;
        return trusted;
    }
}
