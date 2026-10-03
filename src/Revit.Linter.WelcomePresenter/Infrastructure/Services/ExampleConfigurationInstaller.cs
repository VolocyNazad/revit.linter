using Microsoft.Extensions.Logging;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Abstractions.Services;
using System.Globalization;
using System.IO;
using System.Text;

namespace Revit.Linter.WelcomePresenter.Infrastructure.Services;

internal sealed class ExampleConfigurationInstaller : IExampleConfigurationInstaller
{
    internal const string AsideFolderName = "examples";

    private const string ResourcePrefix = "Revit.Linter.WelcomePresenter.Examples.";
    private const int FirstRevitVersionWithGroupTypeIds = 2024;

    private static readonly string[] FileNames =
        ["config.yaml", "collision.config.yaml", "parameter-element.config.yaml"];

    private readonly ILogger<ExampleConfigurationInstaller> _logger;

    public ExampleConfigurationInstaller(ILogger<ExampleConfigurationInstaller> logger) => _logger = logger;

    public string TargetDirectory => ConfigurationPathUtils.Directory;

    public ExampleInstallationResult Install(IReadOnlyCollection<ExampleDiscipline> disciplines)
    {
        string language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru" ? "ru" : "en";
        ExampleInstallationResult result = Install(
            TargetDirectory,
            disciplines,
            fileName => ReadTemplate(language, fileName),
            ConfigurationPathUtils.RevitVersion < FirstRevitVersionWithGroupTypeIds);

        _logger.LogInformation(
            "Installed example configurations for {Disciplines} into {Directory}: {Files}",
            disciplines, TargetDirectory, result.Files.Select(file => file.Path));
        return result;
    }

    /// <summary>
    /// Installs the examples into an explicit directory from an explicit template source.
    /// </summary>
    /// <remarks>Separated from the Revit-version and culture lookup so it can be exercised headlessly.</remarks>
    internal static ExampleInstallationResult Install(
        string targetDirectory,
        IReadOnlyCollection<ExampleDiscipline> disciplines,
        Func<string, string> readTemplate,
        bool useLegacyParameterGroups)
    {
        if (disciplines.Count == 0)
            throw new ArgumentException("At least one discipline must be selected.", nameof(disciplines));

        string[] sections = disciplines.Select(ToSectionName).Distinct().ToArray();
        UTF8Encoding encoding = new(encoderShouldEmitUTF8Identifier: false);
        List<InstalledExampleFile> files = new(FileNames.Length);

        Directory.CreateDirectory(targetDirectory);
        foreach (string fileName in FileNames)
        {
            string content = ExampleConfigurationComposer.Compose(
                readTemplate(fileName), sections, useLegacyParameterGroups);

            string path = Path.Combine(targetDirectory, fileName);
            bool placedAside = File.Exists(path) && !string.IsNullOrWhiteSpace(File.ReadAllText(path));
            if (placedAside)
            {
                string asideDirectory = Path.Combine(targetDirectory, AsideFolderName);
                Directory.CreateDirectory(asideDirectory);
                path = Path.Combine(asideDirectory, fileName);
            }

            File.WriteAllText(path, content, encoding);
            files.Add(new InstalledExampleFile(fileName, path, placedAside));
        }

        return new ExampleInstallationResult(files);
    }

    private static string ToSectionName(ExampleDiscipline discipline) => discipline switch
    {
        ExampleDiscipline.Mep => "mep",
        ExampleDiscipline.Architecture => "architecture",
        ExampleDiscipline.Structure => "structure",
        _ => throw new ArgumentOutOfRangeException(nameof(discipline), discipline, "Unknown example discipline."),
    };

    private static string ReadTemplate(string language, string fileName)
    {
        string resourceName = $"{ResourcePrefix}{language}.{fileName}";
        using Stream stream = typeof(ExampleConfigurationInstaller).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"The example configuration '{resourceName}' is not embedded.");
        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
