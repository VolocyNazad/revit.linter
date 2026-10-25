using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.DiagnosticConfiguration.Models;
using Revit.Linter.DiagnosticConfiguration.Parsing;
using YamlDotNet.Core;

namespace Revit.Linter.DiagnosticConfiguration.Tests;

public sealed class DiagnosticConfigurationReaderTests
{
    [Fact]
    public void Read_reads_the_envelope_and_the_rules()
    {
        const string yaml = """
            version: 2
            rules:
              - type: custom
                code: "CSTM001"
                takeDocument: "!property('IsFamilyDocument')"
                take: "instance"
                check: "true"
            """;

        DiagnosticConfigurationFile? file = DiagnosticConfigurationReader.Read(yaml);

        Assert.NotNull(file);
        Assert.Equal(2, file.Version);
        DiagnosticRule rule = Assert.Single(file.Rules);
        Assert.Equal("custom", rule.Type);
        Assert.Equal("CSTM001", rule.Code);
    }

    [Fact]
    public void Read_returns_null_for_empty_text()
        => Assert.Null(DiagnosticConfigurationReader.Read(" "));

    [Fact]
    public void ReadFile_returns_null_for_a_missing_file()
        => Assert.Null(DiagnosticConfigurationReader.ReadFile(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "diagnostics.config.yaml")));

    [Fact]
    public void Read_reports_invalid_yaml()
        => Assert.ThrowsAny<YamlException>(() => DiagnosticConfigurationReader.Read("rules: [\n"));

    [Fact]
    public void TryRead_converts_a_yaml_failure_into_a_failed_result()
    {
        bool loaded = DiagnosticConfigurationReader.TryRead("rules: [\n", out _, out Exception? error);

        Assert.False(loaded);
        Assert.IsAssignableFrom<YamlException>(error);
    }

    [Fact]
    public void TryReadFile_converts_a_yaml_failure_into_a_failed_result()
    {
        string directory = Path.Combine(Path.GetTempPath(), nameof(DiagnosticConfigurationReaderTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "diagnostics.config.yaml");
        File.WriteAllText(path, "rules: [\n");
        try
        {
            bool loaded = DiagnosticConfigurationReader.TryReadFile(path, out DiagnosticConfigurationFile? file, out Exception? error);

            Assert.False(loaded);
            Assert.Null(file);
            Assert.IsAssignableFrom<YamlException>(error);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
