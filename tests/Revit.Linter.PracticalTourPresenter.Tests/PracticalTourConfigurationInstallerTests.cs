using Revit.Linter.ConfigurationPath;
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
