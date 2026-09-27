using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Revit.Linter.Localization.Generator.Tests;

public sealed class LocalizationResourceIntegrityTests
{
    private static readonly Regex PlaceholderRegex = new(
        @"(?<!\{)\{[^{}]+\}(?!\})",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    [Fact]
    public void Russian_resources_match_neutral_keys_and_placeholders()
    {
        string sourceDirectory = Path.Combine(FindRepositoryRoot(), "src");
        string[] neutralFiles = Directory.GetFiles(sourceDirectory, "*.resx", SearchOption.AllDirectories)
            .Where(path => !Path.GetFileNameWithoutExtension(path).Contains('.'))
            .ToArray();

        Assert.NotEmpty(neutralFiles);

        foreach (string neutralPath in neutralFiles)
        {
            string russianPath = Path.Combine(
                Path.GetDirectoryName(neutralPath)!,
                $"{Path.GetFileNameWithoutExtension(neutralPath)}.ru-RU.resx");
            Assert.True(File.Exists(russianPath), $"Russian resource file was not found for {neutralPath}");

            IReadOnlyDictionary<string, string> neutral = ReadResources(neutralPath);
            IReadOnlyDictionary<string, string> russian = ReadResources(russianPath);

            Assert.Equal(
                neutral.Keys.OrderBy(key => key, StringComparer.Ordinal),
                russian.Keys.OrderBy(key => key, StringComparer.Ordinal));
            foreach (string key in neutral.Keys)
            {
                Assert.Equal(
                    GetPlaceholders(neutral[key]),
                    GetPlaceholders(russian[key]));
            }
        }
    }

    private static IReadOnlyDictionary<string, string> ReadResources(string path) =>
        XDocument.Load(path)
            .Root!
            .Elements("data")
            .ToDictionary(
                element => element.Attribute("name")!.Value,
                element => element.Element("value")?.Value ?? string.Empty,
                StringComparer.Ordinal);

    private static string[] GetPlaceholders(string value) => PlaceholderRegex
        .Matches(value)
        .Cast<Match>()
        .Select(match => match.Value)
        .OrderBy(placeholder => placeholder, StringComparer.Ordinal)
        .ToArray();

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Revit.Linter.slnx")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not find the Revit.Linter repository root.");
    }
}
