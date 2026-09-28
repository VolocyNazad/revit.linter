using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Xml.Linq;
using Xunit;

namespace Revit.Linter.Localization.Artifact.Tests;

public sealed class LocalizationArtifactTests
{
    private const string ViewModelResourceBaseName =
        "Revit.Linter.Localization.DiagnosticReportPresenter.ViewModels.DiagnosticReportViewModel";
    private const string GlobalResourceBaseName = "Revit.Linter.Localization.GlobalLocalizations";
    private const string OpenedDocumentsResourceBaseName =
        "Revit.Linter.Localization.OpenedDocuments.ViewModels.OpenedDocumentsViewModel";
    private const string ElementDiagnosticsResourceBaseName =
        "Revit.Linter.Localization.ElementDiagnostics.ElementDiagnosticLocalizations";
    private const string DiagnosticSeverityResourceBaseName =
        "Revit.Linter.Localization.DiagnosticSeverityLocalizations";
    private const string DiagnosticTargetTypeResourceBaseName =
        "Revit.Linter.Localization.DiagnosticTargetTypeLocalizations";

    [Fact]
    public void Repacked_output_resolves_neutral_russian_and_fallback_resources()
    {
        string artifactDirectory = FindArtifactDirectory();
        string localizationAssemblyPath = Path.Combine(artifactDirectory, "Revit.Linter.Localization.dll");
        string russianSatellitePath = Path.Combine(
            artifactDirectory,
            "ru",
            "Revit.Linter.Localization.resources.dll");

        Assert.True(File.Exists(localizationAssemblyPath), $"Localization assembly was not found: {localizationAssemblyPath}");
        Assert.True(File.Exists(russianSatellitePath), $"Russian satellite assembly was not found: {russianSatellitePath}");

        Assembly localizationAssembly = Assembly.LoadFrom(localizationAssemblyPath);
        Assembly russianSatelliteAssembly = Assembly.LoadFrom(russianSatellitePath);
        NeutralResourcesLanguageAttribute? neutralLanguage =
            localizationAssembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>();
        Assert.NotNull(neutralLanguage);
        Assert.Equal("en", neutralLanguage.CultureName);

        string[] expectedResourceNames = GetExpectedResourceNames();
        Assert.Equal(
            expectedResourceNames,
            localizationAssembly.GetManifestResourceNames().OrderBy(name => name, StringComparer.Ordinal));
        Assert.Equal(
            expectedResourceNames,
            russianSatelliteAssembly.GetManifestResourceNames().OrderBy(name => name, StringComparer.Ordinal));

        Type readerType = localizationAssembly.GetType(
            "Revit.Linter.Localization.LocalizationResourceReader",
            throwOnError: true)!;
        MethodInfo getString = readerType.GetMethod(
            "GetString",
            BindingFlags.Public | BindingFlags.Static)!;

        Assert.Equal("Reports:", ReadString(getString, ViewModelResourceBaseName, "reports_text", "en-US"));
        Assert.Equal("Отчеты:", ReadString(getString, ViewModelResourceBaseName, "reports_text", "ru"));
        Assert.Equal("Отчеты:", ReadString(getString, ViewModelResourceBaseName, "reports_text", "ru-RU"));
        Assert.Equal("Отчеты:", ReadString(getString, ViewModelResourceBaseName, "reports_text", "ru-KZ"));
        Assert.Equal("Reports:", ReadString(getString, ViewModelResourceBaseName, "reports_text", "de-DE"));

        Assert.Equal("Diagnostics", ReadString(getString, GlobalResourceBaseName, "ribbonPanel_diagnostics_name", "en-US"));
        Assert.Equal("Проверки", ReadString(getString, GlobalResourceBaseName, "ribbonPanel_diagnostics_name", "ru-RU"));
        Assert.Equal("Проверки", ReadString(getString, GlobalResourceBaseName, "ribbonPanel_diagnostics_name", "ru-KZ"));
        Assert.Equal("Diagnostics", ReadString(getString, GlobalResourceBaseName, "ribbonPanel_diagnostics_name", "de-DE"));

        Assert.Equal("All", ReadString(getString, OpenedDocumentsResourceBaseName, "allDocuments_text", "en-US"));
        Assert.Equal("Все", ReadString(getString, OpenedDocumentsResourceBaseName, "allDocuments_text", "ru-RU"));
        Assert.Equal("Delete unused material", ReadString(getString, ElementDiagnosticsResourceBaseName, "deleteUnusedMaterial_fix", "en-US"));
        Assert.Equal("Удалить неиспользуемый материал", ReadString(getString, ElementDiagnosticsResourceBaseName, "deleteUnusedMaterial_fix", "ru-RU"));
        Assert.Equal("Warning", ReadString(getString, DiagnosticSeverityResourceBaseName, "Warning", "en-US"));
        Assert.Equal("Предупреждение", ReadString(getString, DiagnosticSeverityResourceBaseName, "Warning", "ru-RU"));
        Assert.Equal("Document", ReadString(getString, DiagnosticTargetTypeResourceBaseName, "Document", "en-US"));
        Assert.Equal("Документ", ReadString(getString, DiagnosticTargetTypeResourceBaseName, "Document", "ru-RU"));
    }

    private static string ReadString(MethodInfo getString, string baseName, string key, string cultureName)
    {
        CultureInfo previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            return (string)getString.Invoke(null, [baseName, key])!;
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

    private static string[] GetExpectedResourceNames()
    {
        string projectPath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Revit.Linter.Localization",
            "Revit.Linter.Localization.csproj");

        return XDocument.Load(projectPath)
            .Descendants("EmbeddedResource")
            .Select(element => element.Attribute("LogicalName")?.Value)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }
}
