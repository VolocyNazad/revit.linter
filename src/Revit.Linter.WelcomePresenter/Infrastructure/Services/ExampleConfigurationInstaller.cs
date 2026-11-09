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

    private const string ResourcePrefix = "Revit.Linter.WelcomePresenter.Examples.";
    private const string CheckResourcePrefix = "Revit.Linter.WelcomePresenter.TemplateChecks.";
    private const string ForgeCheckResourcePrefix = "Revit.Linter.WelcomePresenter.TemplateChecks2024.";
    private const int FirstRevitVersionWithGroupTypeIds = 2024;

    public string TargetDirectory => paths.Directory;

    public ExampleInstallationResult Install(IReadOnlyCollection<ExampleDiscipline> disciplines) =>
        Install(disciplines, []);

    public ExampleInstallationResult Install(
        IReadOnlyCollection<ExampleDiscipline> disciplines, IReadOnlyCollection<TemplateCheckPack> packs)
    {
        string language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru" ? "ru" : "en";
        bool useLegacyParameterGroups = paths.RevitVersion < FirstRevitVersionWithGroupTypeIds;
        ExampleInstallationResult result = Install(
            TargetDirectory,
            disciplines,
            packs,
            fileName => ReadTemplate(language, fileName),
            packs.Count == 0 ? null : pack => ReadCheckBlock(language, pack, useLegacyParameterGroups),
            useLegacyParameterGroups);

        logger.LogInformation(
            "Installed the example configuration for {Disciplines} and check packs {Packs} into {Directory}: {Files}",
            disciplines, packs, TargetDirectory, result.Files.Select(file => file.Path));
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
        bool useLegacyParameterGroups) =>
        Install(targetDirectory, disciplines, [], readTemplate, null, useLegacyParameterGroups);

    /// <summary>
    /// Installs the example and the selected template check blocks into the examples subfolder.
    /// </summary>
    /// <remarks>Separated from the Revit-version and culture lookup so it can be exercised headlessly.</remarks>
    internal static ExampleInstallationResult Install(
        string targetDirectory,
        IReadOnlyCollection<ExampleDiscipline> disciplines,
        IReadOnlyCollection<TemplateCheckPack> packs,
        Func<string, string> readTemplate,
        Func<TemplateCheckPack, string>? readCheckBlock,
        bool useLegacyParameterGroups)
    {
        if (disciplines.Count == 0 && packs.Count == 0)
            throw new ArgumentException("At least one discipline or check pack must be selected.");

        string[] sections = disciplines.Distinct().Select(ToSectionName).ToArray();
        UTF8Encoding encoding = new(encoderShouldEmitUTF8Identifier: false);

        string examplesDirectory = Path.Combine(targetDirectory, ExamplesFolderName);
        Directory.CreateDirectory(examplesDirectory);

        string content = ExampleConfigurationComposer.Compose(
            readTemplate(ExampleFileName), sections, useLegacyParameterGroups);
        if (packs.Count > 0)
        {
            content = TemplateCheckComposer.Append(
                content,
                packs.Distinct().OrderBy(TemplateCheckComposer.GetCodeNumber).ToArray(),
                readCheckBlock ?? throw new ArgumentNullException(nameof(readCheckBlock)));
        }

        string path = Path.Combine(examplesDirectory, ExampleFileName);

        // The examples folder is inside the user's configuration folder. An identical file means the example
        // is already installed, so it is left untouched; a different or empty file is replaced, and a
        // non-empty user-edited copy is preserved as a backup first.
        ExampleInstallationOutcome outcome = ExampleInstallationOutcome.Installed;
        string? backupPath = null;
        if (File.Exists(path))
        {
            string existing = File.ReadAllText(path);
            if (string.Equals(existing, content, StringComparison.Ordinal))
            {
                return new ExampleInstallationResult(
                [
                    new InstalledExampleFile(ExampleFileName, path)
                    {
                        Outcome = ExampleInstallationOutcome.Unchanged,
                    },
                ]);
            }

            if (!string.IsNullOrWhiteSpace(existing))
            {
                backupPath = CreateUniquePath($"{path}.bak-{DateTime.Now:yyyyMMdd-HHmmss}");
                File.Copy(path, backupPath, overwrite: false);
                outcome = ExampleInstallationOutcome.Replaced;
            }
        }

        File.WriteAllText(path, content, encoding);
        return new ExampleInstallationResult(
        [
            new InstalledExampleFile(ExampleFileName, path)
            {
                Outcome = outcome,
                BackupPath = backupPath,
            },
        ]);
    }

    private static string CreateUniquePath(string candidate)
    {
        if (!File.Exists(candidate)) return candidate;

        string? directory = Path.GetDirectoryName(candidate);
        string name = Path.GetFileNameWithoutExtension(candidate);
        string extension = Path.GetExtension(candidate);
        int index = 2;
        string next;
        do
        {
            next = Path.Combine(directory ?? string.Empty, $"{name}-{index}{extension}");
            index++;
        }
        while (File.Exists(next));

        return next;
    }

    private static string ToSectionName(ExampleDiscipline discipline) => discipline switch
    {
        ExampleDiscipline.Mep => "mep",
        ExampleDiscipline.Architecture => "architecture",
        ExampleDiscipline.Structure => "structure",
        _ => throw new ArgumentOutOfRangeException(nameof(discipline), discipline, "Unknown example discipline."),
    };

    private static string ReadTemplate(string language, string fileName) =>
        ReadResource($"{ResourcePrefix}{language}.{fileName}", "example configuration");

    private static string ReadCheckBlock(string language, TemplateCheckPack pack, bool useLegacyParameterGroups) =>
        ReadResource(
            $"{(useLegacyParameterGroups ? CheckResourcePrefix : ForgeCheckResourcePrefix)}" +
            $"{language}.{TemplateCheckComposer.GetFileName(pack)}",
            "template check block");

    private static string ReadResource(string resourceName, string description)
    {
        using Stream stream = typeof(ExampleConfigurationInstaller).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"The {description} '{resourceName}' is not embedded.");
        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
