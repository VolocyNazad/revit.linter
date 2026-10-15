namespace Revit.Linter.ConfigurationPath.Tests;

public sealed class ConfigurationPathUtilsTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        nameof(ConfigurationPathUtilsTests),
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void RevitVersion_matches_the_year_of_the_build_configuration()
    {
#if IS2021
        const int expected = 2021;
#elif IS2022
        const int expected = 2022;
#elif IS2023
        const int expected = 2023;
#elif IS2024
        const int expected = 2024;
#elif IS2025
        const int expected = 2025;
#elif IS2026
        const int expected = 2026;
#elif IS2027
        const int expected = 2027;
#else
        Assert.Fail("The test build targets an unknown Revit year.");
        return;
#endif

        Assert.Equal(expected, ConfigurationPathUtils.RevitVersion);
    }

    [Fact]
    public void EnsureFileExists_creates_parent_directories_and_file()
    {
        string path = GetPath("nested", "configuration.yaml");

        ConfigurationPathUtils.EnsureFileExists(path);

        Assert.True(File.Exists(path));
        Assert.Empty(File.ReadAllText(path));
    }

    [Fact]
    public void EnsureFileExists_does_not_overwrite_existing_file()
    {
        string path = GetPath("configuration.yaml");
        Directory.CreateDirectory(_tempDirectory);
        File.WriteAllText(path, "existing content");

        ConfigurationPathUtils.EnsureFileExists(path);

        Assert.Equal("existing content", File.ReadAllText(path));
    }

    [Fact]
    public void GetConfigurations_creates_missing_file_and_returns_null()
    {
        string path = GetPath("nested", "configuration.yaml");

        TestConfiguration? result = ConfigurationPathUtils.GetConfigurations<TestConfiguration>(path);

        Assert.Null(result);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void GetConfigurations_returns_null_for_empty_file()
    {
        string path = GetPath("configuration.yaml");
        Directory.CreateDirectory(_tempDirectory);
        File.WriteAllText(path, string.Empty);

        TestConfiguration? result = ConfigurationPathUtils.GetConfigurations<TestConfiguration>(path);

        Assert.Null(result);
    }

    [Fact]
    public void GetConfigurations_deserializes_camel_case_yaml()
    {
        string path = GetPath("configuration.yaml");
        Directory.CreateDirectory(_tempDirectory);
        File.WriteAllText(path, """
            displayName: Wall checks
            isActive: true
            codes:
              - WALL-001
              - WALL-002
            """);

        TestConfiguration? result = ConfigurationPathUtils.GetConfigurations<TestConfiguration>(path);

        Assert.NotNull(result);
        Assert.Equal("Wall checks", result.DisplayName);
        Assert.True(result.IsActive);
        Assert.Equal(["WALL-001", "WALL-002"], result.Codes);
    }

    [Fact]
    public void GetConfigurations_reads_updated_file_content()
    {
        string path = GetPath("configuration.yaml");
        Directory.CreateDirectory(_tempDirectory);
        File.WriteAllText(path, "displayName: First");
        _ = ConfigurationPathUtils.GetConfigurations<TestConfiguration>(path);
        File.WriteAllText(path, "displayName: Second");

        TestConfiguration? result = ConfigurationPathUtils.GetConfigurations<TestConfiguration>(path);

        Assert.NotNull(result);
        Assert.Equal("Second", result.DisplayName);
    }

    [Fact]
    public void TryGetConfigurations_returns_error_for_unknown_field()
    {
        string path = GetPath("configuration.yaml");
        Directory.CreateDirectory(_tempDirectory);
        File.WriteAllText(path, "fieldThatDoesNotExist: value");

        bool loaded = ConfigurationPathUtils.TryGetConfigurations<TestConfiguration>(
            path, out TestConfiguration? result, out Exception? error);

        Assert.False(loaded);
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Contains("fieldThatDoesNotExist", error.Message, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory)) Directory.Delete(_tempDirectory, recursive: true);
    }

    private string GetPath(params string[] parts) =>
        parts.Aggregate(_tempDirectory, Path.Combine);

    public sealed class TestConfiguration
    {
        public string? DisplayName { get; set; }
        public bool IsActive { get; set; }
        public IList<string> Codes { get; set; } = [];
    }
}
