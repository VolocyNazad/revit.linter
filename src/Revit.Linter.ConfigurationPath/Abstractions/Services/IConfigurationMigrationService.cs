using Revit.Linter.ConfigurationPath.Abstractions.Models;

namespace Revit.Linter.ConfigurationPath.Abstractions.Services;

/// <summary>
/// Moves the legacy split configuration files of every Revit version into a <c>legacy</c> folder once.
/// </summary>
/// <remarks>
/// The migration covers all version folders under the Revit Linter documents directory, not only the version
/// that is running, so a user who updates one add-in build migrates every Revit version at once. Files are
/// moved, never deleted. A file that cannot be moved stays where it is and is reported as remaining, because
/// the product no longer reads it either way. The product never imports rules, switches or ignores.
/// The migration is tracked by <c>docs/migrations/0001-single-configuration-file.md</c> (MIG-0001) and is
/// removed once that entry's removal gate is met.
/// </remarks>
public interface IConfigurationMigrationService
{
    /// <summary>Moves the legacy configuration files of every Revit version folder into its <c>legacy</c> folder.</summary>
    /// <returns>The files that were moved and the files that could not be moved.</returns>
    /// <remarks>
    /// The call is safe to repeat: a folder that was already migrated contains no legacy file at the top level
    /// and the already moved files stay in the <c>legacy</c> folder. Call it once per add-in session before
    /// anything reads the diagnostic configuration.
    /// </remarks>
    ConfigurationMigrationResult Migrate();
}
