using Microsoft.Extensions.Logging;
using Revit.Linter.ConfigurationPath.Abstractions.Models;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
using Revit.Linter.Core.Abstractions;
using System.IO;

namespace Revit.Linter.ConfigurationPath.Services;

/// <summary>Moves the legacy split configuration files of every Revit version folder into a <c>legacy</c> folder.</summary>
/// <remarks>
/// The files are moved, never deleted, so the user can restore a previous product version from the
/// <c>legacy</c> folder. The migration runs before any configuration is read; after it the product reads only
/// <c>diagnostics.config.yaml</c>, which is why the legacy files are inert even when a move failed. Only
/// four-digit release-year folders are migrated, so a manual backup copy next to them is left untouched.
/// </remarks>
public sealed class ConfigurationMigrationService(
    IConfigurationPaths paths,
    ILogger<ConfigurationMigrationService> logger) : IConfigurationMigrationService
{
    /// <summary>Gets the name of the folder that receives the moved legacy files.</summary>
    /// <remarks>
    /// The folder is created inside every Revit version folder, for example
    /// <c>Documents\Revit Linter\2025\legacy</c>. It preserves the <c>examples</c> and <c>debug</c> subfolders of
    /// the moved files, so a user can restore them to their original locations.
    /// </remarks>
    public const string LegacyFolderName = "legacy";

    private const string LegacyReadmeFileName = "README.txt";

    /// <inheritdoc />
    public ConfigurationMigrationResult Migrate()
    {
        if (!Directory.Exists(paths.VersionsDirectory)) return ConfigurationMigrationResult.None;

        List<string> movedFiles = [];
        List<string> remainingFiles = [];
        HashSet<string> versionsWithMoves = new(StringComparer.OrdinalIgnoreCase);
        foreach (string versionDirectory in Directory.EnumerateDirectories(paths.VersionsDirectory)
                     .Where(IsVersionDirectory))
        {
            List<string> movedFromVersion = [];
            foreach (string legacyPath in ConfigurationPathUtils.GetLegacyConfigurationPaths(versionDirectory))
            {
                if (!File.Exists(legacyPath)) continue;

                string destination = GetDestinationPath(versionDirectory, legacyPath);
                try
                {
                    string? destinationDirectory = Path.GetDirectoryName(destination);
                    if (destinationDirectory is not null)
                        Directory.CreateDirectory(destinationDirectory);

                    string uniqueDestination = CreateUniquePath(destination);
                    File.Move(legacyPath, uniqueDestination);
                    movedFiles.Add(uniqueDestination);
                    movedFromVersion.Add(GetRelativePath(versionDirectory, legacyPath));
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    // A legacy file that cannot be moved simply stays where it is: the product does not read it
                    // either way, and the startup warning reports that the folder still contains an old file.
                    remainingFiles.Add(legacyPath);
                    logger.LogWarning(
                        exception,
                        "Failed to move the legacy configuration file {LegacyPath} into the legacy folder",
                        legacyPath);
                }
            }

            if (movedFromVersion.Count > 0) versionsWithMoves.Add(versionDirectory);
            WriteLegacyReadme(versionDirectory, movedFromVersion);
        }

        if (movedFiles.Count > 0)
            logger.LogInformation(
                "Moved {MovedCount} legacy configuration files into the legacy folders of {VersionCount} Revit versions",
                movedFiles.Count,
                versionsWithMoves.Count);

        if (remainingFiles.Count > 0)
            logger.LogWarning(
                "{RemainingCount} legacy configuration files could not be moved and stay in their folders",
                remainingFiles.Count);

        return new ConfigurationMigrationResult(movedFiles, remainingFiles);
    }

    private static string GetDestinationPath(string versionDirectory, string legacyPath) =>
        Path.Combine(versionDirectory, LegacyFolderName, GetRelativePath(versionDirectory, legacyPath));

    /// <summary>Returns the path of a file relative to its version folder.</summary>
    /// <remarks>
    /// <c>Path.GetRelativePath</c> is unavailable in the .NET Standard 2.0 and .NET Framework targets this
    /// module builds for, and the caller already knows the paths share the same folder, so a prefix match is
    /// sufficient.
    /// </remarks>
    private static string GetRelativePath(string baseDirectory, string path)
    {
        string prefix = baseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        + Path.DirectorySeparatorChar;
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? path.Substring(prefix.Length)
            : Path.GetFileName(path);
    }

    /// <summary>Returns whether a folder name is a Revit release year, for example <c>2025</c>.</summary>
    /// <remarks>
    /// Only version folders are migrated. A manual copy of the configuration folder placed next to them, such
    /// as <c>2025-backup</c>, is left alone.
    /// </remarks>
    private static bool IsVersionDirectory(string directory)
    {
        string name = Path.GetFileName(directory);
        return name.Length == 4 && name.All(character => character >= '0' && character <= '9');
    }

    private static string CreateUniquePath(string candidate)
    {
        if (!File.Exists(candidate)) return candidate;

        string? directory = Path.GetDirectoryName(candidate);
        string name = Path.GetFileNameWithoutExtension(candidate);
        string extension = Path.GetExtension(candidate);
        int index = 2;
        string next;
        do
        {
            next = Path.Combine(directory ?? string.Empty, $"{name}-{index}{extension}");
            index++;
        }
        while (File.Exists(next));

        return next;
    }

    private void WriteLegacyReadme(string versionDirectory, IReadOnlyCollection<string> movedFiles)
    {
        if (movedFiles.Count == 0) return;

        string readmePath = Path.Combine(versionDirectory, LegacyFolderName, LegacyReadmeFileName);
        if (File.Exists(readmePath)) return;

        try
        {
            List<string> lines =
            [
                "Revit Linter moved the old split configuration files from this Revit version folder here.",
                string.Empty,
                "The product does not read these files anymore. Their rules are not loaded and the switches",
                "saved for them do not apply. Move the rules you still need into diagnostics.config.yaml by hand;",
                "the migration guide explains how:",
                ProductIdentity.WikiRootUrl + "Migrating-configuration",
                string.Empty,
                "Moved files:",
            ];
            foreach (string relativePath in movedFiles.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                lines.Add("* " + relativePath);
            lines.Add(string.Empty);
            lines.Add("Nothing was deleted. To return to an older product version, move these files back to their");
            lines.Add("original locations first.");

            File.WriteAllText(readmePath, string.Join(Environment.NewLine, lines) + Environment.NewLine);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The explanation is a convenience; the moved files stay usable without it.
            logger.LogDebug(exception, "Failed to write the legacy folder explanation at {ReadmePath}", readmePath);
        }
    }
}
