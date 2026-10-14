using Revit.Linter.ConfigurationPath.Abstractions.Models;
using YamlDotNet.Core;

namespace Revit.Linter.ConfigurationPath.Tests;

public sealed class VersionedConfigurationFileTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        nameof(VersionedConfigurationFileTests),
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Enveloped_file_deserializes_with_its_version()
    {
        string path = Write("configuration.yaml", """
            version: 1
            rules:
              - code: FIRST
              - code: SECOND
            """);

        ConfigurationFile<TestRule>? file = ConfigurationPathUtils.GetVersionedConfigurations<TestRule>(path);

        Assert.NotNull(file);
        Assert.Equal(1, file.Version);
        Assert.Equal(["FIRST", "SECOND"], file.Rules.Select(rule => rule.Code).ToArray());
    }

    [Fact]
    public void Legacy_bare_list_reads_as_version_zero()
    {
        string path = Write("configuration.yaml", """
            - code: FIRST
            - code: SECOND
            """);

        ConfigurationFile<TestRule>? file = ConfigurationPathUtils.GetVersionedConfigurations<TestRule>(path);

        Assert.NotNull(file);
        Assert.Equal(0, file.Version);
        Assert.Equal(["FIRST", "SECOND"], file.Rules.Select(rule => rule.Code).ToArray());
    }

    [Fact]
    public void Mapping_without_version_defaults_to_zero()
    {
        string path = Write("configuration.yaml", """
            rules:
              - code: FIRST
            """);

        ConfigurationFile<TestRule>? file = ConfigurationPathUtils.GetVersionedConfigurations<TestRule>(path);

        Assert.NotNull(file);
        Assert.Equal(0, file.Version);
        Assert.Equal(["FIRST"], file.Rules.Select(rule => rule.Code).ToArray());
    }

    [Fact]
    public void Newer_version_is_preserved_for_compatibility_checks()
    {
        int newer = ConfigurationPathUtils.SupportedConfigurationVersion + 1;
        string path = Write("configuration.yaml", $"""
            version: {newer}
            rules:
              - code: FIRST
            """);

        ConfigurationFile<TestRule>? file = ConfigurationPathUtils.GetVersionedConfigurations<TestRule>(path);

        Assert.NotNull(file);
        Assert.Equal(newer, file.Version);
        Assert.Equal(["FIRST"], file.Rules.Select(rule => rule.Code).ToArray());
    }

    [Fact]
    public void Empty_rules_read_as_empty_list()
    {
        string path = Write("configuration.yaml", """
            version: 1
            rules: []
            """);

        ConfigurationFile<TestRule>? file = ConfigurationPathUtils.GetVersionedConfigurations<TestRule>(path);

        Assert.NotNull(file);
        Assert.Equal(1, file.Version);
        Assert.Empty(file.Rules);
    }

    [Fact]
    public void Empty_file_returns_null()
    {
        string path = Write("configuration.yaml", string.Empty);

        ConfigurationFile<TestRule>? file = ConfigurationPathUtils.GetVersionedConfigurations<TestRule>(path);

        Assert.Null(file);
    }

    [Fact]
    public void Invalid_yaml_throws_on_get_and_fails_on_try()
    {
        string path = Write("configuration.yaml", "- code: [unclosed");

        Assert.Throws<YamlException>(() => ConfigurationPathUtils.GetVersionedConfigurations<TestRule>(path));

        bool loaded = ConfigurationPathUtils.TryGetVersionedConfigurations<TestRule>(
            path, out ConfigurationFile<TestRule>? file, out Exception? error);

        Assert.False(loaded);
        Assert.Null(file);
        Assert.NotNull(error);
    }

    [Fact]
    public void Try_get_returns_envelope_on_success()
    {
        string path = Write("configuration.yaml", """
            version: 1
            rules:
              - code: FIRST
            """);

        bool loaded = ConfigurationPathUtils.TryGetVersionedConfigurations<TestRule>(
            path, out ConfigurationFile<TestRule>? file, out Exception? error);

        Assert.True(loaded);
        Assert.Null(error);
        Assert.Equal(1, file?.Version);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory)) Directory.Delete(_tempDirectory, recursive: true);
    }

    private string Write(string fileName, string content)
    {
        Directory.CreateDirectory(_tempDirectory);
        string path = Path.Combine(_tempDirectory, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    public sealed class TestRule
    {
        public string Code { get; set; } = string.Empty;
    }
}
