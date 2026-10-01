namespace Revit.Linter.Updater.Core.Models;

/// <summary>Contains effective updater behavior after settings and policies are resolved.</summary>
public sealed record UpdaterConfiguration(
    bool ChecksEnabled,
    bool AutomaticChecksEnabled,
    bool NotificationsEnabled,
    TimeSpan AutomaticCheckInterval,
    Uri ReleaseApiUri)
{
    /// <summary>The built-in public endpoint for the latest stable release.</summary>
    public static readonly Uri DefaultReleaseApiUri = new(
        "https://api.github.com/repos/VolocyNazad/revit.linter/releases/latest");

    /// <summary>The built-in minimum interval between successful automatic checks.</summary>
    public static readonly TimeSpan DefaultAutomaticCheckInterval = TimeSpan.FromHours(24);
}
