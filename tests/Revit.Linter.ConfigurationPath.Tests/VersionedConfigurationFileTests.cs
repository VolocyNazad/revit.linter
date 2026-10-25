using Revit.Linter.ConfigurationPath.Abstractions.Models;
using Revit.Linter.ConfigurationPath.Services;
using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.ConfigurationPath.Tests;

public sealed class VersionedConfigurationFileTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), nameof(VersionedConfigurationFileTests), Guid.NewGuid().ToString("N"));

    public VersionedConfigurationFileTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup of a test directory.
        }
    }

    [Fact]
    public void Service_reads_version_two_rules_in_source_order()
    {
        string user = WriteFile("diagnostics.config.yaml", """
            version: 2
            rules:
              - type: custom
                code: "CSTM001"
                takeDocument: "!property('IsFamilyDocument')"
                take: "instance"
                check: "true"
            """);
        string example = WriteFile(Path.Combine("examples", "diagnostics.config.yaml"), """
            version: 2
            rules:
              - type: collision
                code: "CLSN101"
                takeDocument: "!property('IsFamilyDocument')"
                take: "instance"
                andTake: "instance"
                groupBy: "'all'"
            """);

        using var service = CreateService(user, example);

        IReadOnlyList<DiagnosticRuleSource> rules = service.GetRules();

        Assert.Equal(2, rules.Count);
        Assert.Equal("custom", rules[0].Rule.Type);
        Assert.Equal("collision", rules[1].Rule.Type);
        Assert.True(rules[1].IsExample);
    }

    [Fact]
    public void Service_skips_a_file_with_a_newer_version_and_reports_it()
    {
        string user = WriteFile("diagnostics.config.yaml", "version: 3\nrules: []\n");
        var errors = new DiagnosticConfigurationErrorState();
        using var service = CreateService(errors, user);

        Assert.Empty(service.GetRules());
        Assert.Contains("newer", errors.CurrentError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Service_skips_a_file_with_an_unsupported_version()
    {
        string user = WriteFile("diagnostics.config.yaml", "version: 1\nrules: []\n");
        var errors = new DiagnosticConfigurationErrorState();
        using var service = CreateService(errors, user);

        Assert.Empty(service.GetRules());
        Assert.Contains("unsupported format version", errors.CurrentError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Service_keeps_valid_rules_and_reports_invalid_ones()
    {
        string user = WriteFile("diagnostics.config.yaml", """
            version: 2
            rules:
              - type: custom
                code: "BROKEN"
                take: "instance"
              - type: custom
                code: "CSTM001"
                takeDocument: "!property('IsFamilyDocument')"
                take: "instance"
                check: "true"
              - type: unknown
                code: "WHAT"
            """);
        var errors = new DiagnosticConfigurationErrorState();
        using var service = CreateService(errors, user);

        DiagnosticRuleSource rule = Assert.Single(service.GetRules());
        Assert.Equal("CSTM001", rule.Rule.Code);
        Assert.Contains("BROKEN", errors.CurrentError, StringComparison.Ordinal);
        Assert.Contains("WHAT", errors.CurrentError, StringComparison.Ordinal);
    }

    [Fact]
    public void Service_skips_missing_debug_and_example_files_without_creating_them()
    {
        string user = WriteFile("diagnostics.config.yaml", "version: 2\nrules: []\n");
        using var service = CreateService(user);

        Assert.Empty(service.GetRules());
        Assert.False(File.Exists(Path.Combine(_directory, ConfigurationPathUtils.DebugFolderName,
            ConfigurationPathUtils.DiagnosticsConfigFileName)));
    }

    [Fact]
    public void Service_creates_the_top_level_file_when_it_is_missing()
    {
        string user = Path.Combine(_directory, ConfigurationPathUtils.DiagnosticsConfigFileName);
        using var service = CreateService(user);

        Assert.Empty(service.GetRules());
        Assert.True(File.Exists(user));
        Assert.Contains("version: 2", File.ReadAllText(user), StringComparison.Ordinal);
    }

    [Fact]
    public void Service_validates_built_in_entries()
    {
        string user = WriteFile("diagnostics.config.yaml", """
            version: 2
            rules:
              - type: builtIn
                code: "SHRD003"
                isActive: false
                severity: "Warning"
                isHidden: true
            """);
        var errors = new DiagnosticConfigurationErrorState();
        using var service = CreateService(errors, user);

        DiagnosticRule rule = Assert.Single(service.GetRules()).Rule;
        Assert.Equal("builtIn", rule.Type);
        Assert.False(rule.IsActive);
        Assert.Equal(DiagnosticSeverity.Warning, rule.Severity);
        Assert.True(rule.IsHidden);
        Assert.Null(errors.CurrentError);
    }

    [Fact]
    public void Service_rejects_definition_fields_on_a_built_in_entry()
    {
        string user = WriteFile("diagnostics.config.yaml", """
            version: 2
            rules:
              - type: builtIn
                code: "SHRD003"
                take: "instance"
            """);
        var errors = new DiagnosticConfigurationErrorState();
        using var service = CreateService(errors, user);

        Assert.Empty(service.GetRules());
        Assert.Contains("take", errors.CurrentError, StringComparison.Ordinal);
    }

    private static DiagnosticConfigurationService CreateService(params string[] paths)
        => CreateService(new DiagnosticConfigurationErrorState(), paths);

    private static DiagnosticConfigurationService CreateService(
        DiagnosticConfigurationErrorState errors, params string[] paths)
    {
        List<DiagnosticConfigurationFileDescriptor> files = [];
        foreach (string path in paths)
        {
            bool isExample = path.Contains(
                $"{Path.DirectorySeparatorChar}{ConfigurationPathUtils.ExamplesFolderName}{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase);
            bool isDebug = path.Contains(
                $"{Path.DirectorySeparatorChar}{ConfigurationPathUtils.DebugFolderName}{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase);
            files.Add(new DiagnosticConfigurationFileDescriptor(
                path, IsExample: isExample, IsDebug: isDebug, IsTour: false, IsTopLevel: files.Count == 0));
        }

        return new DiagnosticConfigurationService(files, errors);
    }

    private string WriteFile(string name, string content)
    {
        string path = Path.Combine(_directory, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }
}
