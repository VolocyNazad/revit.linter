using System.Globalization;
using System.Reflection;
using System.Resources;
using Xunit;

namespace Revit.Linter.Localization.Artifact.Tests;

public sealed class LocalizationArtifactTests
{
    private const string ResourceBaseName =
        "Revit.Linter.Localization.DiagnosticReportPresenter.ViewModels.DiagnosticReportViewModel";

    [Fact]
    public void Repacked_output_resolves_neutral_russian_and_fallback_resources()
    {
        string artifactDirectory = FindArtifactDirectory();
        string localizationAssemblyPath = Path.Combine(artifactDirectory, "Revit.Linter.Localization.dll");
        string russianSatellitePath = Path.Combine(
            artifactDirectory,
            "ru-RU",
            "Revit.Linter.Localization.resources.dll");

        Assert.True(File.Exists(localizationAssemblyPath), $"Localization assembly was not found: {localizationAssemblyPath}");
        Assert.True(File.Exists(russianSatellitePath), $"Russian satellite assembly was not found: {russianSatellitePath}");

        Assembly localizationAssembly = Assembly.LoadFrom(localizationAssemblyPath);
        NeutralResourcesLanguageAttribute? neutralLanguage =
            localizationAssembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>();
        Assert.NotNull(neutralLanguage);
        Assert.Equal("en", neutralLanguage.CultureName);

        Type readerType = localizationAssembly.GetType(
            "Revit.Linter.Localization.LocalizationResourceReader",
            throwOnError: true)!;
        MethodInfo getString = readerType.GetMethod(
            "GetString",
            BindingFlags.Public | BindingFlags.Static)!;

        Assert.Equal("Reports:", ReadString(getString, "en-US"));
        Assert.Equal("Отчеты:", ReadString(getString, "ru-RU"));
        Assert.Equal("Reports:", ReadString(getString, "de-DE"));
    }

    private static string ReadString(MethodInfo getString, string cultureName)
    {
        CultureInfo previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            return (string)getString.Invoke(null, [ResourceBaseName, "reports_text"])!;
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    private static string FindArtifactDirectory()
    {
        string configuration = typeof(LocalizationArtifactTests).Assembly
            .GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration
            ?? throw new InvalidOperationException("The test assembly has no build configuration metadata.");
        string root = FindRepositoryRoot();
        string configurationDirectory = Path.Combine(
            root,
            "src",
            "Revit.Linter",
            "bin",
            "x64",
            configuration);

        Assert.True(
            Directory.Exists(configurationDirectory),
            $"The add-in output directory was not found: {configurationDirectory}");

        string[] candidates = Directory.GetDirectories(configurationDirectory)
            .Where(directory => File.Exists(Path.Combine(directory, "Revit.Linter.dll")))
            .ToArray();

        Assert.Single(candidates);
        return candidates[0];
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Revit.Linter.slnx")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not find the Revit.Linter repository root.");
    }
}
