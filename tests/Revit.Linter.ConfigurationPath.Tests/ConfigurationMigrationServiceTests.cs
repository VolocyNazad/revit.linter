using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.ConfigurationPath.Abstractions.Models;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
using Revit.Linter.ConfigurationPath.Services;

namespace Revit.Linter.ConfigurationPath.Tests;

public sealed class ConfigurationMigrationServiceTests : IDisposable
{
    private const int RevitVersion = 2025;

    private readonly string _documentsDirectory = Path.Combine(
        Path.GetTempPath(), nameof(ConfigurationMigrationServiceTests), Guid.NewGuid().ToString("N"));

    public ConfigurationMigrationServiceTests() => Directory.CreateDirectory(_documentsDirectory);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_documentsDirectory)) Directory.Delete(_documentsDirectory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup of a test directory.
        }
    }

    [Fact]
    public void Migrates_the_legacy_files_of_every_revit_version_at_once()
    {
        string version2024 = VersionDirectory(2024);
        string version2025 = VersionDirectory(2025);
        PlaceLegacyFile(version2024, ConfigurationPathUtils.LegacyConfigFileName);
        PlaceLegacyFile(
            version2024,
            Path.Combine(ConfigurationPathUtils.ExamplesFolderName, ConfigurationPathUtils.LegacyCollisionConfigFileName));
        PlaceLegacyFile(
            version2024,
            Path.Combine(ConfigurationPathUtils.DebugFolderName, ConfigurationPathUtils.LegacyParameterElementConfigFileName));
        PlaceLegacyFile(version2025, ConfigurationPathUtils.LegacyConfigFileName);
        string currentFormatPath = Path.Combine(version2025, ConfigurationPathUtils.DiagnosticsConfigFileName);
        File.WriteAllText(currentFormatPath, "version: 2\nrules: []\n");
        ConfigurationMigrationService service = CreateService();

        ConfigurationMigrationResult result = service.Migrate();

        Assert.Equal(4, result.MovedFiles.Count);
        Assert.Empty(result.RemainingFiles);
        Assert.False(File.Exists(Path.Combine(version2024, ConfigurationPathUtils.LegacyConfigFileName)));
        Assert.False(File.Exists(Path.Combine(version2025, ConfigurationPathUtils.LegacyConfigFileName)));
        Assert.True(File.Exists(Path.Combine(
            version2024, ConfigurationMigrationService.LegacyFolderName, ConfigurationPathUtils.LegacyConfigFileName)));
        Assert.True(File.Exists(Path.Combine(
            version2024,
            ConfigurationMigrationService.LegacyFolderName,
            ConfigurationPathUtils.ExamplesFolderName,
            ConfigurationPathUtils.LegacyCollisionConfigFileName)));
        Assert.True(File.Exists(Path.Combine(
            version2024,
            ConfigurationMigrationService.LegacyFolderName,
            ConfigurationPathUtils.DebugFolderName,
            ConfigurationPathUtils.LegacyParameterElementConfigFileName)));
        Assert.True(File.Exists(Path.Combine(
            version2025, ConfigurationMigrationService.LegacyFolderName, ConfigurationPathUtils.LegacyConfigFileName)));
        Assert.Equal("version: 2\nrules: []\n", File.ReadAllText(currentFormatPath));
        Assert.True(File.Exists(Path.Combine(
            version2024, ConfigurationMigrationService.LegacyFolderName, "README.txt")));
    }

    [Fact]
    public void A_migrated_folder_is_left_alone_on_the_next_start()
    {
        string versionDirectory = VersionDirectory(RevitVersion);
        PlaceLegacyFile(versionDirectory, ConfigurationPathUtils.LegacyConfigFileName);
        ConfigurationMigrationService service = CreateService();
        service.Migrate();

        ConfigurationMigrationResult result = service.Migrate();

        Assert.False(result.HasFindings);
        Assert.True(File.Exists(Path.Combine(
            versionDirectory, ConfigurationMigrationService.LegacyFolderName, ConfigurationPathUtils.LegacyConfigFileName)));
    }

    [Fact]
    public void An_existing_legacy_file_gets_a_unique_destination()
    {
        string versionDirectory = VersionDirectory(RevitVersion);
        string legacyDirectory = Path.Combine(versionDirectory, ConfigurationMigrationService.LegacyFolderName);
        Directory.CreateDirectory(legacyDirectory);
        File.WriteAllText(Path.Combine(legacyDirectory, ConfigurationPathUtils.LegacyConfigFileName), "moved earlier");
        PlaceLegacyFile(versionDirectory, ConfigurationPathUtils.LegacyConfigFileName);
        ConfigurationMigrationService service = CreateService();

        ConfigurationMigrationResult result = service.Migrate();

        Assert.Single(result.MovedFiles);
        Assert.Equal("moved earlier", File.ReadAllText(Path.Combine(legacyDirectory, ConfigurationPathUtils.LegacyConfigFileName)));
        Assert.True(File.Exists(Path.Combine(legacyDirectory, "config-2.yaml")));
    }

    [Fact]
    public void A_locked_legacy_file_stays_in_place_and_is_reported()
    {
        string versionDirectory = VersionDirectory(RevitVersion);
        string legacyPath = PlaceLegacyFile(versionDirectory, ConfigurationPathUtils.LegacyConfigFileName);
        ConfigurationMigrationService service = CreateService();

        using (File.Open(legacyPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            ConfigurationMigrationResult result = service.Migrate();

            Assert.Empty(result.MovedFiles);
            Assert.Equal([legacyPath], result.RemainingFiles);
        }

        Assert.True(File.Exists(legacyPath));
        Assert.False(File.Exists(Path.Combine(
            versionDirectory, ConfigurationMigrationService.LegacyFolderName, ConfigurationPathUtils.LegacyConfigFileName)));
    }

    [Fact]
    public void A_folder_without_legacy_files_is_untouched()
    {
        string versionDirectory = VersionDirectory(RevitVersion);
        Directory.CreateDirectory(versionDirectory);
        File.WriteAllText(Path.Combine(versionDirectory, ConfigurationPathUtils.DiagnosticsConfigFileName), "version: 2\n");
        ConfigurationMigrationService service = CreateService();

        ConfigurationMigrationResult result = service.Migrate();

        Assert.False(result.HasFindings);
        Assert.False(Directory.Exists(Path.Combine(versionDirectory, ConfigurationMigrationService.LegacyFolderName)));
    }

    [Fact]
    public void A_manual_backup_folder_next_to_the_version_folders_is_left_alone()
    {
        string backupDirectory = Path.Combine(_documentsDirectory, "Revit Linter", "2025-backup");
        PlaceLegacyFile(backupDirectory, ConfigurationPathUtils.LegacyConfigFileName);
        ConfigurationMigrationService service = CreateService();

        ConfigurationMigrationResult result = service.Migrate();

        Assert.False(result.HasFindings);
        Assert.True(File.Exists(Path.Combine(backupDirectory, ConfigurationPathUtils.LegacyConfigFileName)));
        Assert.False(Directory.Exists(Path.Combine(backupDirectory, ConfigurationMigrationService.LegacyFolderName)));
    }

    private string VersionDirectory(int revitVersion) => CreatePaths(revitVersion).Directory;

    private static string PlaceLegacyFile(string versionDirectory, string relativePath)
    {
        string path = Path.Combine(versionDirectory, relativePath);
        string? directory = Path.GetDirectoryName(path);
        if (directory is not null) Directory.CreateDirectory(directory);
        File.WriteAllText(path, "- code: \"OLD001\"\n");
        return path;
    }

    private ConfigurationPaths CreatePaths(int revitVersion) => new(revitVersion, _documentsDirectory);

    private ConfigurationMigrationService CreateService() =>
        new(CreatePaths(RevitVersion), NullLogger<ConfigurationMigrationService>.Instance);
}
