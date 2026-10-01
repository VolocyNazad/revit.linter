namespace Revit.Linter.Updater.Core.Models;

/// <summary>Describes the observable outcome of an update check.</summary>
public enum UpdateCheckStatus
{
    /// <summary>Automatic checks are disabled by user settings.</summary>
    Disabled,

    /// <summary>The configured automatic-check interval has not elapsed.</summary>
    NotDue,

    /// <summary>No newer stable release exists.</summary>
    UpToDate,

    /// <summary>A newer stable release is available.</summary>
    UpdateAvailable,

    /// <summary>The available release was explicitly skipped by the user.</summary>
    Skipped,

    /// <summary>The release request or persisted-state operation failed.</summary>
    Failed
}
