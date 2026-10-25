using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.ConfigurationPath.Services;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.DiagnosticConfiguration.Models;
using Revit.Linter.PracticalTourPresenter.Infrastructure.Services;
using Revit.Linter.Testing;

namespace Revit.Linter.PracticalTourPresenter.Tests;

public sealed class PracticalTourConfigurationInstallerTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        nameof(PracticalTourConfigurationInstallerTests),
        Guid.NewGuid().ToString("N"));

    public PracticalTourConfigurationInstallerTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory)) Directory.Delete(_tempDirectory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup of a test directory.
        }
    }

    [Fact]
    public void Practical_tour_is_installed_and_removed_as_a_managed_file()
    {
        string staleExample = Path.Combine(
            _tempDirectory, ConfigurationPathUtils.ExamplesFolderName, ConfigurationPathUtils.PracticalTourFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(staleExample)!);
        File.WriteAllText(staleExample, "stale");

        string path = PracticalTourConfigurationInstaller.Install(
            _tempDirectory,
            fileName => File.ReadAllText(Path.Combine(
                RepositoryRoot.Find(), "wiki", "examples", "configuration", fileName)));

        Assert.Equal(
            Path.Combine(_tempDirectory, ConfigurationPathUtils.TourFolderName, ConfigurationPathUtils.PracticalTourFileName),
            path);
        Assert.False(File.Exists(staleExample));
        DiagnosticConfigurationFile file = Assert.IsType<DiagnosticConfigurationFile>(
            ConfigurationPathUtils.GetDiagnosticConfiguration(path));
        DiagnosticRule rule = Assert.Single(file.Rules);
        Assert.Equal("TOUR001", rule.Code);
        Assert.Equal("collision", rule.Type);
        Assert.False(rule.IsActive);
        Assert.Equal(3, rule.Visualizations.Length);

        Assert.True(PracticalTourConfigurationInstaller.Remove(_tempDirectory));
        Assert.False(File.Exists(path));
        Assert.False(PracticalTourConfigurationInstaller.Remove(_tempDirectory));
    }

    [Fact]
    public void Refresh_outdated_replaces_a_tour_file_in_the_old_format()
    {
        ConfigurationPaths paths = CreatePaths();
        Directory.CreateDirectory(Path.GetDirectoryName(paths.PracticalTourConfigPath)!);
        File.WriteAllText(paths.PracticalTourConfigPath, "- code: \"TOUR001\"\n  description: \"old format\"\n");
        PracticalTourConfigurationInstaller installer = CreateInstaller(paths);

        Assert.True(installer.RefreshOutdated());

        DiagnosticConfigurationFile file = Assert.IsType<DiagnosticConfigurationFile>(
            ConfigurationPathUtils.GetDiagnosticConfiguration(paths.PracticalTourConfigPath));
        Assert.Equal(ConfigurationPathUtils.SupportedConfigurationVersion, file.Version);
        Assert.Contains(file.Rules, rule => rule.Code == "TOUR001");
    }

    [Fact]
    public void Refresh_outdated_keeps_a_current_tour_file()
    {
        ConfigurationPaths paths = CreatePaths();
        PracticalTourConfigurationInstaller installer = CreateInstaller(paths);
        installer.Install();
        string installed = File.ReadAllText(paths.PracticalTourConfigPath);

        Assert.False(installer.RefreshOutdated());

        Assert.Equal(installed, File.ReadAllText(paths.PracticalTourConfigPath));
    }

    private ConfigurationPaths CreatePaths() => new(2025, _tempDirectory);

    private static PracticalTourConfigurationInstaller CreateInstaller(ConfigurationPaths paths) =>
        new(NullLogger<PracticalTourConfigurationInstaller>.Instance, paths);

    [Fact]
    public void Every_language_embeds_the_managed_tour_configuration()
    {
        string[] resources = typeof(PracticalTourConfigurationInstaller).Assembly.GetManifestResourceNames();
        foreach (string language in new[] { "en", "ru" })
        {
            Assert.Contains(
                $"Revit.Linter.PracticalTourPresenter.Examples.{language}.{ConfigurationPathUtils.PracticalTourFileName}",
                resources);
        }
    }
}
