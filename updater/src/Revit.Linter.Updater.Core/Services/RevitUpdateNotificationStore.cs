using Revit.Linter.Core.Abstractions.Models;
using System.Text.Json;

namespace Revit.Linter.Updater.Core.Services;

/// <summary>Writes the one-shot startup result that the Revit add-in reads.</summary>
/// <remarks>
/// The add-in passes an explicit result path when it starts the updater. The file is written
/// atomically and removed by the add-in after it has been read.
/// </remarks>
public static class RevitUpdateNotificationStore
{
    /// <summary>Writes the available release to the requested result path.</summary>
    /// <param name="path">The absolute result file path supplied by the add-in.</param>
    /// <param name="notification">The notification to persist.</param>
    /// <param name="cancellationToken">A token that cancels the write.</param>
    public static async Task WriteAsync(
        string path,
        RevitUpdateNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(notification);

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        string temporaryPath = path + ".tmp";
        await File.WriteAllTextAsync(
            temporaryPath,
            JsonSerializer.Serialize(notification),
            cancellationToken);
        File.Move(temporaryPath, path, true);
    }
}
