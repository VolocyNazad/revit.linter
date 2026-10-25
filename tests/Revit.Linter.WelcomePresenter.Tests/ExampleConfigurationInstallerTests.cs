using Revit.Linter.ConfigurationPath;
using Revit.Linter.DiagnosticConfiguration.Models;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Testing;
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
        Assert.Equal(ExampleInstallationOutcome.Installed, installed.Outcome);
        Assert.Null(installed.BackupPath);
        DiagnosticConfigurationFile file = Assert.IsType<DiagnosticConfigurationFile>(
            ConfigurationPathUtils.GetDiagnosticConfiguration(installed.Path));
        Assert.Equal(2, file.Version);
        Assert.Equal(custom, file.Rules.Count(rule => rule.Type == "custom"));
        Assert.Equal(collision, file.Rules.Count(rule => rule.Type == "collision"));
        Assert.Equal(parameter, file.Rules.Count(rule => rule.Type == "parameter"));
        Assert.DoesNotContain(file.Rules, rule => rule.Type == "builtIn");
    }

    [Fact]
    public void Install_leaves_the_legacy_example_files_to_the_startup_migration()
    {
        string examplesDirectory = Path.Combine(_tempDirectory, ConfigurationPathUtils.ExamplesFolderName);
        Directory.CreateDirectory(examplesDirectory);
        (string Name, string Content)[] legacyContent =
        [
            (ConfigurationPathUtils.LegacyConfigFileName, "- code: \"OLD001\"\n"),
            (ConfigurationPathUtils.LegacyCollisionConfigFileName, "- code: \"OLD002\"\n"),
            (ConfigurationPathUtils.LegacyParameterElementConfigFileName, "- code: \"OLD003\"\n"),
        ];
        foreach ((string name, string content) in legacyContent)
            File.WriteAllText(Path.Combine(examplesDirectory, name), content);

        ExampleConfigurationInstaller.Install(
            _tempDirectory,
            [ExampleDiscipline.Mep],
            fileName => File.ReadAllText(Path.Combine(
                RepositoryRoot.Find(), "wiki", "examples", "configuration", fileName)),
            useLegacyParameterGroups: false);

        foreach ((string name, string content) in legacyContent)
            Assert.Equal(content, File.ReadAllText(Path.Combine(examplesDirectory, name)));
        Assert.True(File.Exists(Path.Combine(examplesDirectory, ConfigurationPathUtils.DiagnosticsConfigFileName)));
    }

    [Fact]
    public void Install_keeps_a_backup_of_an_existing_example_file()
    {
        string examplesDirectory = Path.Combine(_tempDirectory, ConfigurationPathUtils.ExamplesFolderName);
        Directory.CreateDirectory(examplesDirectory);
        string path = Path.Combine(examplesDirectory, ConfigurationPathUtils.DiagnosticsConfigFileName);
        File.WriteAllText(path, "# my own copy\nversion: 2\nrules: []\n");

        ExampleInstallationResult result = ExampleConfigurationInstaller.Install(
            _tempDirectory,
            [ExampleDiscipline.Mep],
            fileName => File.ReadAllText(Path.Combine(
                RepositoryRoot.Find(), "wiki", "examples", "configuration", fileName)),
            useLegacyParameterGroups: false);

        string backup = Assert.Single(Directory.GetFiles(examplesDirectory, "*.bak-*"));
        InstalledExampleFile installed = Assert.Single(result.Files);
        Assert.Equal(ExampleInstallationOutcome.Replaced, installed.Outcome);
        Assert.Equal(backup, installed.BackupPath);
        Assert.Contains("# my own copy", File.ReadAllText(backup), StringComparison.Ordinal);
        Assert.DoesNotContain("# my own copy", File.ReadAllText(path), StringComparison.Ordinal);
    }

    [Fact]
    public void Install_does_not_rewrite_an_identical_example_file()
    {
        Func<string, string> readTemplate = fileName => File.ReadAllText(Path.Combine(
            RepositoryRoot.Find(), "wiki", "examples", "configuration", fileName));
        ExampleConfigurationInstaller.Install(
            _tempDirectory, [ExampleDiscipline.Mep], readTemplate, useLegacyParameterGroups: false);
        string examplesDirectory = Path.Combine(_tempDirectory, ConfigurationPathUtils.ExamplesFolderName);
        string path = Path.Combine(examplesDirectory, ConfigurationPathUtils.DiagnosticsConfigFileName);
        string installed = File.ReadAllText(path);

        ExampleInstallationResult second = ExampleConfigurationInstaller.Install(
            _tempDirectory, [ExampleDiscipline.Mep], readTemplate, useLegacyParameterGroups: false);

        Assert.Equal(installed, File.ReadAllText(path));
        Assert.Empty(Directory.GetFiles(examplesDirectory, "*.bak-*"));
        InstalledExampleFile installedFile = Assert.Single(second.Files);
        Assert.Equal(ExampleInstallationOutcome.Unchanged, installedFile.Outcome);
        Assert.Null(installedFile.BackupPath);
    }

    [Fact]
    public void Install_replaces_an_empty_example_file_without_a_backup()
    {
        string examplesDirectory = Path.Combine(_tempDirectory, ConfigurationPathUtils.ExamplesFolderName);
        Directory.CreateDirectory(examplesDirectory);
        string path = Path.Combine(examplesDirectory, ConfigurationPathUtils.DiagnosticsConfigFileName);
        File.WriteAllText(path, "  " + Environment.NewLine);

        ExampleInstallationResult result = ExampleConfigurationInstaller.Install(
            _tempDirectory,
            [ExampleDiscipline.Mep],
            fileName => File.ReadAllText(Path.Combine(
                RepositoryRoot.Find(), "wiki", "examples", "configuration", fileName)),
            useLegacyParameterGroups: false);

        Assert.Contains("version: 2", File.ReadAllText(path), StringComparison.Ordinal);
        Assert.Empty(Directory.GetFiles(examplesDirectory, "*.bak-*"));
        InstalledExampleFile installed = Assert.Single(result.Files);
        Assert.Equal(ExampleInstallationOutcome.Installed, installed.Outcome);
        Assert.Null(installed.BackupPath);
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
    public void Every_example_is_embedded_per_language()
    {
        string[] resources = typeof(ExampleConfigurationInstaller).Assembly.GetManifestResourceNames();
        foreach (string language in new[] { "en", "ru" })
        {
            Assert.Contains(
                $"Revit.Linter.WelcomePresenter.Examples.{language}.{ConfigurationPathUtils.DiagnosticsConfigFileName}",
                resources);
        }
    }
}
