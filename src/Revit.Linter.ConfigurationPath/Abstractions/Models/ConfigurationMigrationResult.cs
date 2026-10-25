namespace Revit.Linter.ConfigurationPath.Abstractions.Models;

/// <summary>
/// Describes what the one-time legacy configuration migration moved and what it could not move.
/// </summary>
/// <param name="MovedFiles">The destination paths of the files that were moved into a <c>legacy</c> folder.</param>
/// <param name="RemainingFiles">The source paths of the legacy files that are still in place because they could not be moved.</param>
/// <remarks>
/// A remaining file is never read by the product: it simply stays where it was, and the startup warning tells
/// the user that the folder still contains an old file. An empty result means the configuration folders were
/// already migrated.
/// </remarks>
public sealed record ConfigurationMigrationResult(
    IReadOnlyList<string> MovedFiles,
    IReadOnlyList<string> RemainingFiles)
{
    /// <summary>Gets an empty result, used when there is nothing to migrate.</summary>
    public static ConfigurationMigrationResult None { get; } = new([], []);

    /// <summary>Gets a value indicating whether any legacy file was found.</summary>
    public bool HasFindings => MovedFiles.Count > 0 || RemainingFiles.Count > 0;
}
