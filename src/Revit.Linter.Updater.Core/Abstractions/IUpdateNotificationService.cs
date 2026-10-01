using Revit.Linter.Updater.Core.Models;

namespace Revit.Linter.Updater.Core.Abstractions;

/// <summary>Displays a user-visible notification for an available release.</summary>
public interface IUpdateNotificationService
{
    /// <summary>Attempts to display an update notification without taking application focus.</summary>
    /// <returns><see langword="true"/> only when the notification was submitted successfully.</returns>
    Task<bool> TryShowAsync(ReleaseInfo release, CancellationToken cancellationToken = default);
}
