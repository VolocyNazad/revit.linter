using Revit.Linter.ConfigurationPath.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.ConfigurationPath.Tests;

public sealed class ConfigurationPathUtilsTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), nameof(ConfigurationPathUtilsTests), Guid.NewGuid().ToString("N"));

    public ConfigurationPathUtilsTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup of a test directory.
        }
    }

    [Fact]
    public void RevitVersion_matches_the_year_of_the_build_configuration()
    {
        Assert.InRange(ConfigurationPathUtils.RevitVersion, 2021, 2027);
    }

    [Fact]
    public void EnsureConfigurationFile_creates_a_version_two_header()
    {
        string path = Path.Combine(_directory, "nested", ConfigurationPathUtils.DiagnosticsConfigFileName);

        ConfigurationPathUtils.EnsureConfigurationFile(path);

        Assert.True(File.Exists(path));
        string content = File.ReadAllText(path);
        Assert.Contains("version: 2", content, StringComparison.Ordinal);
        Assert.Contains("rules: []", content, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureConfigurationFile_does_not_overwrite_an_existing_file()
    {
        string path = Path.Combine(_directory, ConfigurationPathUtils.DiagnosticsConfigFileName);
        File.WriteAllText(path, "version: 2\nrules: []\n");

        ConfigurationPathUtils.EnsureConfigurationFile(path);

        Assert.Equal("version: 2\nrules: []\n", File.ReadAllText(path));
    }

    [Fact]
    public void GetDiagnosticConfiguration_reads_the_envelope_and_rules()
    {
        string path = Path.Combine(_directory, ConfigurationPathUtils.DiagnosticsConfigFileName);
        File.WriteAllText(path, """
            version: 2
            rules:
              - type: custom
                code: "CSTM001"
                takeDocument: "!property('IsFamilyDocument')"
                take: "instance"
                check: "true"
            """);

        DiagnosticConfigurationFile? file = ConfigurationPathUtils.GetDiagnosticConfiguration(path);

        Assert.NotNull(file);
        Assert.Equal(2, file.Version);
        DiagnosticRule rule = Assert.Single(file.Rules);
        Assert.Equal("custom", rule.Type);
        Assert.Equal("CSTM001", rule.Code);
    }

    [Fact]
    public void GetDiagnosticConfiguration_returns_null_for_a_missing_file()
    {
        string path = Path.Combine(_directory, "missing.yaml");

        Assert.Null(ConfigurationPathUtils.GetDiagnosticConfiguration(path));
    }

    [Fact]
    public void TryGetDiagnosticConfiguration_reports_invalid_yaml()
    {
        string path = Path.Combine(_directory, ConfigurationPathUtils.DiagnosticsConfigFileName);
        File.WriteAllText(path, "version: 2\nrules: [\n");

        bool loaded = ConfigurationPathUtils.TryGetDiagnosticConfiguration(path, out _, out Exception? error);

        Assert.False(loaded);
        Assert.NotNull(error);
    }

    [Fact]
    public void Configured_file_names_match_the_single_file_format()
    {
        Assert.Equal("diagnostics.config.yaml", ConfigurationPathUtils.DiagnosticsConfigFileName);
        Assert.EndsWith(
            ConfigurationPathUtils.DiagnosticsConfigFileName, ConfigurationPathUtils.ConfigPath, StringComparison.Ordinal);
        Assert.Contains(ConfigurationPathUtils.DebugFolderName, ConfigurationPathUtils.DebugConfigPath, StringComparison.Ordinal);
        Assert.Contains(ConfigurationPathUtils.ExamplesFolderName, ConfigurationPathUtils.ExampleConfigPath, StringComparison.Ordinal);
        Assert.Contains(ConfigurationPathUtils.TourFolderName, ConfigurationPathUtils.PracticalTourConfigPath, StringComparison.Ordinal);
    }

    [Fact]
    public void Legacy_configured_files_are_kept_only_for_detection()
    {
        Assert.Equal(9, ConfigurationPathUtils.LegacyConfigurationPaths.Count);
        Assert.Contains(ConfigurationPathUtils.LegacyConfigPath, ConfigurationPathUtils.LegacyConfigurationPaths);
        Assert.Contains(ConfigurationPathUtils.LegacyCollisionConfigPath, ConfigurationPathUtils.LegacyConfigurationPaths);
        Assert.Contains(ConfigurationPathUtils.LegacyParameterElementConfigPath, ConfigurationPathUtils.LegacyConfigurationPaths);
    }
}
