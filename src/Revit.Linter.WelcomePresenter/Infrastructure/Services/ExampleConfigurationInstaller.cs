using Microsoft.Extensions.Logging;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Abstractions.Services;
using System.Globalization;
using System.IO;
using System.Text;

namespace Revit.Linter.WelcomePresenter.Infrastructure.Services;

/// <summary>
/// Installs the shipped example configuration for the running Revit version without overwriting user files.
/// </summary>
internal sealed class ExampleConfigurationInstaller(
    ILogger<ExampleConfigurationInstaller> logger,
    IConfigurationPaths paths) : IExampleConfigurationInstaller
{
    internal const string ExamplesFolderName = ConfigurationPathUtils.ExamplesFolderName;
    private const string ExampleFileName = ConfigurationPathUtils.DiagnosticsConfigFileName;

    // Obsolete example files are removed from the product-owned examples folder so the user
    // does not keep a file the product no longer reads.
    private static readonly string[] ObsoleteFileNames =
    [
        ConfigurationPathUtils.LegacyConfigFileName,
        ConfigurationPathUtils.LegacyCollisionConfigFileName,
        ConfigurationPathUtils.LegacyParameterElementConfigFileName,
    ];

    private const string ResourcePrefix = "Revit.Linter.WelcomePresenter.Examples.";
    private const int FirstRevitVersionWithGroupTypeIds = 2024;

    public string TargetDirectory => paths.Directory;

    public ExampleInstallationResult Install(IReadOnlyCollection<ExampleDiscipline> disciplines)
    {
        string language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru" ? "ru" : "en";
        ExampleInstallationResult result = Install(
            TargetDirectory,
            disciplines,
            fileName => ReadTemplate(language, fileName),
            paths.RevitVersion < FirstRevitVersionWithGroupTypeIds);

        logger.LogInformation(
            "Installed the example configuration for {Disciplines} into {Directory}: {Files}",
            disciplines, TargetDirectory, result.Files.Select(file => file.Path));
        return result;
    }

    /// <summary>
    /// Installs the example into the examples subfolder of an explicit directory.
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

        string examplesDirectory = Path.Combine(targetDirectory, ExamplesFolderName);
        Directory.CreateDirectory(examplesDirectory);
        foreach (string obsoleteFileName in ObsoleteFileNames)
        {
            string obsoletePath = Path.Combine(examplesDirectory, obsoleteFileName);
            if (File.Exists(obsoletePath)) File.Delete(obsoletePath);
        }

        string content = ExampleConfigurationComposer.Compose(
            readTemplate(ExampleFileName), sections, useLegacyParameterGroups);
        string path = Path.Combine(examplesDirectory, ExampleFileName);
        File.WriteAllText(path, content, encoding);
        return new ExampleInstallationResult([new InstalledExampleFile(ExampleFileName, path)]);
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
