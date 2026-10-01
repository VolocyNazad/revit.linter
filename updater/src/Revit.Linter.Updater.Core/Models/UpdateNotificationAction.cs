namespace Revit.Linter.Updater.Core.Models;

/// <summary>Identifies an action selected in an update notification.</summary>
public enum UpdateNotificationAction
{
    /// <summary>Opens the release page where the installer can be downloaded.</summary>
    Download,

    /// <summary>Opens the release notes.</summary>
    ReleaseNotes,

    /// <summary>Dismisses the notification without changing persisted preferences.</summary>
    Later,

    /// <summary>Suppresses further notifications for this release version.</summary>
    SkipVersion
}
