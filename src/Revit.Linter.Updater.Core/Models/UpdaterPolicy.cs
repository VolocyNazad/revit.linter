namespace Revit.Linter.Updater.Core.Models;

/// <summary>Contains optional administrator-controlled updater values from one policy scope.</summary>
public sealed class UpdaterPolicy
{
    /// <summary>Gets or sets whether all release checks are allowed.</summary>
    public bool? ChecksEnabled { get; set; }

    /// <summary>Gets or sets whether Windows update notifications are allowed.</summary>
    public bool? NotificationsEnabled { get; set; }

    /// <summary>Gets or sets the interval between automatic checks.</summary>
    public TimeSpan? AutomaticCheckInterval { get; set; }

    /// <summary>Gets or sets the HTTPS endpoint used to query the latest release.</summary>
    public Uri? ReleaseApiUri { get; set; }
}
