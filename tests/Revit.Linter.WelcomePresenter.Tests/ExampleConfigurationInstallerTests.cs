using Revit.Linter.ConfigurationPath;
using Revit.Linter.ConfigurationPath.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Testing;
using Revit.Linter.WelcomePresenter.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Infrastructure.Services;

namespace Revit.Linter.WelcomePresenter.Tests;

public sealed class ExampleConfigurationInstallerTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        nameof(ExampleConfigurationInstallerTests),
        Guid.NewGuid().ToString("N"));

    public static TheoryData<string, ExampleDiscipline[], int, int, int> RealExampleCases => new()
    {
        { "", [ExampleDiscipline.Mep], 6, 5, 1 },
        { "", [ExampleDiscipline.Architecture], 6, 3, 1 },
        { "", [ExampleDiscipline.Structure], 6, 3, 1 },
        { "", [ExampleDiscipline.Mep, ExampleDiscipline.Architecture], 6, 8, 2 },
        { "", [ExampleDiscipline.Mep, ExampleDiscipline.Architecture, ExampleDiscipline.Structure], 6, 11, 3 },
        { "ru", [ExampleDiscipline.Mep], 6, 5, 1 },
        { "ru", [ExampleDiscipline.Mep, ExampleDiscipline.Architecture, ExampleDiscipline.Structure], 6, 11, 3 },
    };

    public ExampleConfigurationInstallerTests() => Directory.CreateDirectory(_tempDirectory);

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

    [Theory]
    [MemberData(nameof(RealExampleCases))]
    public void Real_examples_compose_the_expected_rules(
        string language, ExampleDiscipline[] disciplines, int custom, int collision, int parameter)
    {
        string directory = Path.Combine(_tempDirectory, "real", string.IsNullOrEmpty(language) ? "en" : language);

        ExampleInstallationResult result = ExampleConfigurationInstaller.Install(
            directory,
            disciplines,
            fileName => File.ReadAllText(Path.Combine(
                RepositoryRoot.Find(), "wiki", "examples", "configuration", language, fileName)),
            useLegacyParameterGroups: false);

        InstalledExampleFile installed = Assert.Single(result.Files);
        Assert.Equal(ConfigurationPathUtils.DiagnosticsConfigFileName, installed.FileName);
        DiagnosticConfigurationFile file = Assert.IsType<DiagnosticConfigurationFile>(
            ConfigurationPathUtils.GetDiagnosticConfiguration(installed.Path));
        Assert.Equal(2, file.Version);
        Assert.Equal(custom, file.Rules.Count(rule => rule.Type == "custom"));
        Assert.Equal(collision, file.Rules.Count(rule => rule.Type == "collision"));
        Assert.Equal(parameter, file.Rules.Count(rule => rule.Type == "parameter"));
        Assert.DoesNotContain(file.Rules, rule => rule.Type == "builtIn");
    }

    [Fact]
    public void Install_writes_one_file_and_removes_obsolete_example_files()
    {
        string examplesDirectory = Path.Combine(_tempDirectory, ConfigurationPathUtils.ExamplesFolderName);
        Directory.CreateDirectory(examplesDirectory);
        foreach (string obsolete in new[]
                 {
                     ConfigurationPathUtils.LegacyConfigFileName,
                     ConfigurationPathUtils.LegacyCollisionConfigFileName,
                     ConfigurationPathUtils.LegacyParameterElementConfigFileName,
                 })
        {
            File.WriteAllText(Path.Combine(examplesDirectory, obsolete), "version: 1\nrules: []\n");
        }

        ExampleConfigurationInstaller.Install(
            _tempDirectory,
            [ExampleDiscipline.Mep],
            fileName => File.ReadAllText(Path.Combine(
                RepositoryRoot.Find(), "wiki", "examples", "configuration", fileName)),
            useLegacyParameterGroups: false);

        Assert.Single(Directory.GetFiles(examplesDirectory));
        Assert.True(File.Exists(Path.Combine(examplesDirectory, ConfigurationPathUtils.DiagnosticsConfigFileName)));
    }

    [Fact]
    public void Install_never_touches_the_user_configuration_file()
    {
        string userPath = Path.Combine(_tempDirectory, ConfigurationPathUtils.DiagnosticsConfigFileName);
        File.WriteAllText(userPath, "version: 2\nrules: []\n# user file\n");

        ExampleConfigurationInstaller.Install(
            _tempDirectory,
            [ExampleDiscipline.Mep],
            fileName => File.ReadAllText(Path.Combine(
                RepositoryRoot.Find(), "wiki", "examples", "configuration", fileName)),
            useLegacyParameterGroups: false);

        Assert.Equal("version: 2\nrules: []\n# user file\n", File.ReadAllText(userPath));
    }

    [Fact]
    public void Install_writes_utf8_without_a_byte_order_mark()
    {
        ExampleInstallationResult result = ExampleConfigurationInstaller.Install(
            _tempDirectory,
            [ExampleDiscipline.Mep],
            fileName => File.ReadAllText(Path.Combine(
                RepositoryRoot.Find(), "wiki", "examples", "configuration", fileName)),
            useLegacyParameterGroups: false);

        byte[] bytes = File.ReadAllBytes(result.Files[0].Path);
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
    }

    [Fact]
    public void Install_converts_parameter_groups_for_revit_before_2024()
    {
        ExampleInstallationResult result = ExampleConfigurationInstaller.Install(
            _tempDirectory,
            [ExampleDiscipline.Mep],
            fileName => File.ReadAllText(Path.Combine(
                RepositoryRoot.Find(), "wiki", "examples", "configuration", fileName)),
            useLegacyParameterGroups: true);

        string content = File.ReadAllText(result.Files[0].Path);
        Assert.Contains("group: \"PG_DATA\"", content, StringComparison.Ordinal);
        Assert.DoesNotContain("autodesk.parameter.group:", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Install_rejects_an_empty_discipline_selection()
    {
        Assert.Throws<ArgumentException>(() => ExampleConfigurationInstaller.Install(
            _tempDirectory, [], _ => string.Empty, useLegacyParameterGroups: false));
    }

    [Fact]
    public void Practical_tour_is_installed_and_removed_as_a_managed_file()
    {
        string staleExample = Path.Combine(
            _tempDirectory, ConfigurationPathUtils.ExamplesFolderName, ConfigurationPathUtils.PracticalTourFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(staleExample)!);
        File.WriteAllText(staleExample, "stale");

        string path = ExampleConfigurationInstaller.InstallPracticalTour(
            _tempDirectory,
            fileName => File.ReadAllText(Path.Combine(
                RepositoryRoot.Find(), "wiki", "examples", "configuration", fileName)));

        Assert.Equal(Path.Combine(_tempDirectory, ConfigurationPathUtils.TourFolderName,
            ConfigurationPathUtils.PracticalTourFileName), path);
        Assert.False(File.Exists(staleExample));
        DiagnosticConfigurationFile file = Assert.IsType<DiagnosticConfigurationFile>(
            ConfigurationPathUtils.GetDiagnosticConfiguration(path));
        DiagnosticRule rule = Assert.Single(file.Rules);
        Assert.Equal("TOUR001", rule.Code);
        Assert.Equal("collision", rule.Type);
        Assert.False(rule.IsActive);
        Assert.Equal(3, rule.Visualizations.Length);

        Assert.True(ExampleConfigurationInstaller.RemovePracticalTour(_tempDirectory));
        Assert.False(File.Exists(path));
        Assert.False(ExampleConfigurationInstaller.RemovePracticalTour(_tempDirectory));
    }

    [Fact]
    public void Every_example_is_embedded_per_language()
    {
        string[] resources = typeof(ExampleConfigurationInstaller).Assembly.GetManifestResourceNames();
        foreach (string language in new[] { "en", "ru" })
        {
            Assert.Contains(
                $"Revit.Linter.WelcomePresenter.Examples.{language}.{ConfigurationPathUtils.DiagnosticsConfigFileName}",
                resources);
            Assert.Contains(
                $"Revit.Linter.WelcomePresenter.Examples.{language}.{ConfigurationPathUtils.PracticalTourFileName}",
                resources);
        }
    }
}
