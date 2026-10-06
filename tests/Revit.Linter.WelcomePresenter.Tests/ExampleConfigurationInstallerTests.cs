using Revit.Linter.ConfigurationPath;
using Revit.Linter.Testing;
using Revit.Linter.WelcomePresenter.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Infrastructure.Services;

namespace Revit.Linter.WelcomePresenter.Tests;

public sealed class ExampleConfigurationInstallerTests : IDisposable
{
    private static readonly string[] FileNames =
        ["config.yaml", "collision.config.yaml", "parameter-element.config.yaml"];

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        nameof(ExampleConfigurationInstallerTests),
        Guid.NewGuid().ToString("N"));

    public static TheoryData<string, ExampleDiscipline[], int, int> RealExampleCases => new()
    {
        { "", [ExampleDiscipline.Mep], 3, 1 },
        { "", [ExampleDiscipline.Architecture], 3, 1 },
        { "", [ExampleDiscipline.Structure], 3, 1 },
        { "", [ExampleDiscipline.Mep, ExampleDiscipline.Structure], 6, 2 },
        { "", [ExampleDiscipline.Mep, ExampleDiscipline.Architecture, ExampleDiscipline.Structure], 9, 3 },
        { "ru", [ExampleDiscipline.Architecture], 3, 1 },
        { "ru", [ExampleDiscipline.Mep, ExampleDiscipline.Architecture, ExampleDiscipline.Structure], 9, 3 },
    };

    [Fact]
    public void Installs_every_file_into_a_missing_directory()
    {
        ExampleInstallationResult result = Install([ExampleDiscipline.Mep]);

        Assert.Equal(FileNames, result.Files.Select(file => file.FileName));
        Assert.All(result.Files, file =>
        {
            Assert.False(file.PlacedAside);
            Assert.Equal(Path.Combine(_tempDirectory, file.FileName), file.Path);
            Assert.Equal(FakeTemplate(file.FileName) + Environment.NewLine, File.ReadAllText(file.Path));
        });
    }

    [Fact]
    public void Replaces_an_empty_or_whitespace_file()
    {
        Directory.CreateDirectory(_tempDirectory);
        File.WriteAllText(Path.Combine(_tempDirectory, "config.yaml"), string.Empty);
        File.WriteAllText(Path.Combine(_tempDirectory, "collision.config.yaml"), " \r\n\t");

        ExampleInstallationResult result = Install([ExampleDiscipline.Mep]);

        Assert.All(result.Files, file => Assert.False(file.PlacedAside));
        Assert.StartsWith("# config.yaml", File.ReadAllText(Path.Combine(_tempDirectory, "config.yaml")));
        Assert.StartsWith("# collision.config.yaml", File.ReadAllText(Path.Combine(_tempDirectory, "collision.config.yaml")));
    }

    [Fact]
    public void Never_overwrites_a_file_that_contains_rules()
    {
        Directory.CreateDirectory(_tempDirectory);
        string userFile = Path.Combine(_tempDirectory, "config.yaml");
        File.WriteAllText(userFile, "- code: \"MINE\"");

        ExampleInstallationResult result = Install([ExampleDiscipline.Mep]);

        Assert.Equal("- code: \"MINE\"", File.ReadAllText(userFile));
        InstalledExampleFile aside = Assert.Single(result.Files, file => file.PlacedAside);
        Assert.Equal("config.yaml", aside.FileName);
        Assert.Equal(
            Path.Combine(_tempDirectory, ExampleConfigurationInstaller.AsideFolderName, "config.yaml"), aside.Path);
        Assert.StartsWith("# config.yaml", File.ReadAllText(aside.Path));
    }

    [Fact]
    public void Replaces_an_example_placed_aside_earlier()
    {
        Directory.CreateDirectory(_tempDirectory);
        File.WriteAllText(Path.Combine(_tempDirectory, "config.yaml"), "- code: \"MINE\"");
        Install([ExampleDiscipline.Mep]);
        string asidePath = Path.Combine(_tempDirectory, ExampleConfigurationInstaller.AsideFolderName, "config.yaml");
        File.WriteAllText(asidePath, "stale");

        Install([ExampleDiscipline.Mep]);

        Assert.StartsWith("# config.yaml", File.ReadAllText(asidePath));
    }

    [Fact]
    public void Files_are_written_as_utf8_without_a_byte_order_mark()
    {
        Install([ExampleDiscipline.Mep]);

        byte[] bytes = File.ReadAllBytes(Path.Combine(_tempDirectory, "config.yaml"));
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
    }

    [Fact]
    public void Empty_discipline_selection_is_rejected_before_anything_is_written()
    {
        Assert.Throws<ArgumentException>(() => Install([]));

        Assert.False(Directory.Exists(_tempDirectory));
    }

    [Theory]
    [MemberData(nameof(RealExampleCases))]
    public void Real_examples_deserialize_for_the_selected_disciplines(
        string languageFolder, ExampleDiscipline[] disciplines, int expectedCollisionRules, int expectedParameterRules)
    {
        ExampleConfigurationInstaller.Install(
            _tempDirectory, disciplines, fileName => ReadWikiExample(languageFolder, fileName), useLegacyParameterGroups: false);

        Assert.Equal(6, ReadRules("config.yaml").Count);
        Assert.Equal(expectedCollisionRules, ReadRules("collision.config.yaml").Count);
        Assert.Equal(expectedParameterRules, ReadRules("parameter-element.config.yaml").Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ru")]
    public void Real_parameter_example_uses_only_groups_that_convert_for_revit_before_2024(string languageFolder)
    {
        ExampleConfigurationInstaller.Install(
            _tempDirectory,
            [ExampleDiscipline.Mep, ExampleDiscipline.Architecture, ExampleDiscipline.Structure],
            fileName => ReadWikiExample(languageFolder, fileName),
            useLegacyParameterGroups: true);

        string content = File.ReadAllText(Path.Combine(_tempDirectory, "parameter-element.config.yaml"));
        Assert.Contains("group: \"PG_DATA\"", content);
        Assert.DoesNotContain("group: \"autodesk.parameter.group:", content);
    }

    [Fact]
    public void Practical_tour_install_writes_the_template_into_the_tour_folder()
    {
        string path = ExampleConfigurationInstaller.InstallPracticalTour(_tempDirectory, _ => "# practical-tour");

        Assert.Equal(
            Path.Combine(
                _tempDirectory,
                ExampleConfigurationInstaller.TourFolderName,
                ExampleConfigurationInstaller.PracticalTourFileName),
            path);
        Assert.Equal("# practical-tour", File.ReadAllText(path));
        byte[] bytes = File.ReadAllBytes(path);
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
    }

    [Fact]
    public void Practical_tour_install_removes_the_stale_examples_configuration()
    {
        Directory.CreateDirectory(Path.Combine(_tempDirectory, ExampleConfigurationInstaller.AsideFolderName));
        string stalePath = Path.Combine(
            _tempDirectory,
            ExampleConfigurationInstaller.AsideFolderName,
            ExampleConfigurationInstaller.PracticalTourFileName);
        string siblingPath = Path.Combine(_tempDirectory, ExampleConfigurationInstaller.AsideFolderName, "config.yaml");
        File.WriteAllText(stalePath, "stale");
        File.WriteAllText(siblingPath, "- code: \"MINE\"");

        ExampleConfigurationInstaller.InstallPracticalTour(_tempDirectory, _ => "# practical-tour");

        Assert.False(File.Exists(stalePath));
        Assert.Equal("- code: \"MINE\"", File.ReadAllText(siblingPath));
    }

    [Fact]
    public void Practical_tour_install_refreshes_an_existing_tour_configuration()
    {
        Directory.CreateDirectory(Path.Combine(_tempDirectory, ExampleConfigurationInstaller.TourFolderName));
        File.WriteAllText(
            Path.Combine(
                _tempDirectory,
                ExampleConfigurationInstaller.TourFolderName,
                ExampleConfigurationInstaller.PracticalTourFileName),
            "stale");

        string path = ExampleConfigurationInstaller.InstallPracticalTour(_tempDirectory, _ => "# practical-tour");

        Assert.Equal("# practical-tour", File.ReadAllText(path));
    }

    [Fact]
    public void Practical_tour_remove_deletes_the_installed_configuration()
    {
        Directory.CreateDirectory(Path.Combine(_tempDirectory, ExampleConfigurationInstaller.TourFolderName));
        string path = Path.Combine(
            _tempDirectory,
            ExampleConfigurationInstaller.TourFolderName,
            ExampleConfigurationInstaller.PracticalTourFileName);
        string siblingPath = Path.Combine(_tempDirectory, "config.yaml");
        File.WriteAllText(path, "stale");
        File.WriteAllText(siblingPath, "- code: \"MINE\"");

        bool removed = ExampleConfigurationInstaller.RemovePracticalTour(_tempDirectory);

        Assert.True(removed);
        Assert.False(File.Exists(path));
        Assert.Equal("- code: \"MINE\"", File.ReadAllText(siblingPath));
    }

    [Fact]
    public void Practical_tour_remove_reports_a_missing_configuration()
    {
        bool removed = ExampleConfigurationInstaller.RemovePracticalTour(_tempDirectory);

        Assert.False(removed);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("ru")]
    public void Every_example_is_embedded_for_the_language(string language)
    {
        string[] resourceNames = typeof(IWelcomeWizard).Assembly.GetManifestResourceNames();

        Assert.All(FileNames, fileName =>
            Assert.Contains($"Revit.Linter.WelcomePresenter.Examples.{language}.{fileName}", resourceNames));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory)) Directory.Delete(_tempDirectory, recursive: true);
    }

    private ExampleInstallationResult Install(ExampleDiscipline[] disciplines) =>
        ExampleConfigurationInstaller.Install(_tempDirectory, disciplines, FakeTemplate, useLegacyParameterGroups: false);

    private static string FakeTemplate(string fileName) => $"# {fileName}";

    private static string ReadWikiExample(string languageFolder, string fileName) =>
        File.ReadAllText(Path.Combine(
            RepositoryRoot.Find(), "wiki", "examples", "configuration", languageFolder, fileName));

    private List<Dictionary<string, object>> ReadRules(string fileName) =>
        ConfigurationPathUtils.GetConfigurations<List<Dictionary<string, object>>>(
            Path.Combine(_tempDirectory, fileName))
        ?? [];
}
