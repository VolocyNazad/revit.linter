namespace Revit.Linter.Updater.Core.Models;

/// <summary>Contains per-user updater preferences and check history.</summary>
public sealed class UpdaterState
{
    /// <summary>Gets or sets whether scheduled checks are enabled.</summary>
    public bool AutomaticChecksEnabled { get; set; } = true;

    /// <summary>Gets or sets the UTC time of the last successful release check.</summary>
    public DateTimeOffset? LastCheckedAt { get; set; }

    /// <summary>Gets or sets the last version for which a notification was displayed.</summary>
    public string? LastNotifiedVersion { get; set; }

    /// <summary>Gets or sets the stable version explicitly skipped by the user.</summary>
    public string? SkippedVersion { get; set; }
}
