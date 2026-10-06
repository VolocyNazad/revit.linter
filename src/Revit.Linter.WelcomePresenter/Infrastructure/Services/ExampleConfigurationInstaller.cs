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
    internal const string TourFolderName = "tour";
    internal const string PracticalTourFileName = "practical-tour.config.yaml";

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

    public string InstallPracticalTour()
    {
        string language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru" ? "ru" : "en";
        string stalePath = Path.Combine(TargetDirectory, AsideFolderName, PracticalTourFileName);
        bool hadStaleConfiguration = File.Exists(stalePath);
        string path = InstallPracticalTour(TargetDirectory, fileName => ReadTemplate(language, fileName));
        if (hadStaleConfiguration)
            _logger.LogInformation("Removed the superseded practical-tour configuration at {ConfigurationPath}", stalePath);
        _logger.LogInformation("Installed practical-tour diagnostic configuration into {ConfigurationPath}", path);
        return path;
    }

    public void RemovePracticalTour()
    {
        if (RemovePracticalTour(TargetDirectory))
            _logger.LogInformation("Removed the practical-tour diagnostic configuration.");
    }

    /// <summary>
    /// Removes the practical-tour configuration from an explicit directory.
    /// </summary>
    /// <returns><see langword="true"/> when a file was removed.</returns>
    /// <remarks>Separated from the configuration folder lookup so it can be exercised headlessly.</remarks>
    internal static bool RemovePracticalTour(string targetDirectory)
    {
        string path = Path.Combine(targetDirectory, TourFolderName, PracticalTourFileName);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }

    /// <summary>
    /// Installs the practical-tour configuration into an explicit directory from an explicit template source.
    /// </summary>
    /// <remarks>Separated from the Revit-version and culture lookup so it can be exercised headlessly.</remarks>
    internal static string InstallPracticalTour(string targetDirectory, Func<string, string> readTemplate)
    {
        string tourDirectory = Path.Combine(targetDirectory, TourFolderName);
        Directory.CreateDirectory(tourDirectory);
        string path = Path.Combine(tourDirectory, PracticalTourFileName);
        UTF8Encoding encoding = new(encoderShouldEmitUTF8Identifier: false);
        File.WriteAllText(path, readTemplate(PracticalTourFileName), encoding);
        string stalePath = Path.Combine(targetDirectory, AsideFolderName, PracticalTourFileName);
        if (File.Exists(stalePath)) File.Delete(stalePath);
        return path;
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
