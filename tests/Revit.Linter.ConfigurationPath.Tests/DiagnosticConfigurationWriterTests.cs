using Revit.Linter.ConfigurationPath.Abstractions.Models;
using Revit.Linter.DiagnosticConfiguration.Models;
using Revit.Linter.ConfigurationPath.Services;
using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.ConfigurationPath.Tests;

public sealed class DiagnosticConfigurationWriterTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), nameof(DiagnosticConfigurationWriterTests), Guid.NewGuid().ToString("N"));

    public DiagnosticConfigurationWriterTests() => Directory.CreateDirectory(_directory);

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
    public void UpdateState_changes_fields_and_preserves_comments()
    {
        string path = WriteConfiguration("""
            # Keep this comment.
            version: 2
            rules:
              # The rule comment.
              - type: custom
                code: "CSTM001"
                takeDocument: "!property('IsFamilyDocument')"
                take: "instance"
                check: "true"
            """);
        using DiagnosticConfigurationService service = CreateService(path, out _);
        var writer = new DiagnosticConfigurationWriter(service);

        writer.UpdateState(path, "CSTM001", DiagnosticRuleType.Custom,
            isActive: false, severity: DiagnosticSeverity.Error, isHidden: true);

        string content = File.ReadAllText(path);
        Assert.Contains("# Keep this comment.", content, StringComparison.Ordinal);
        Assert.Contains("# The rule comment.", content, StringComparison.Ordinal);
        Assert.Contains("isActive: false", content, StringComparison.Ordinal);
        Assert.Contains("severity: \"Error\"", content, StringComparison.Ordinal);
        Assert.Contains("isHidden: true", content, StringComparison.Ordinal);
        DiagnosticConfigurationFile? file = ConfigurationPathUtils.GetDiagnosticConfiguration(path);
        DiagnosticRule rule = Assert.Single(file!.Rules);
        Assert.False(rule.IsActive);
        Assert.Equal(DiagnosticSeverity.Error, rule.Severity);
        Assert.True(rule.IsHidden);
    }

    [Fact]
    public void UpdateState_appends_a_built_in_entry_under_the_marker()
    {
        string path = WriteConfiguration("""
            # Header comment.
            version: 2
            rules:
              - type: custom
                code: "CSTM001"
                takeDocument: "!property('IsFamilyDocument')"
                take: "instance"
                check: "true"
            """);
        using DiagnosticConfigurationService service = CreateService(path, out _);
        var writer = new DiagnosticConfigurationWriter(service);

        writer.UpdateState(path, "SHRD003", DiagnosticRuleType.BuiltIn,
            isActive: false, severity: DiagnosticSeverity.Warning, isHidden: false);

        string content = File.ReadAllText(path);
        Assert.Contains("# Built-in overrides", content, StringComparison.Ordinal);
        DiagnosticConfigurationFile? file = ConfigurationPathUtils.GetDiagnosticConfiguration(path);
        Assert.NotNull(file);
        Assert.Equal(2, file.Rules.Count);
        DiagnosticRule builtIn = file.Rules[1];
        Assert.Equal("builtIn", builtIn.Type);
        Assert.Equal("SHRD003", builtIn.Code);
        Assert.False(builtIn.IsActive);
        Assert.Equal(DiagnosticSeverity.Warning, builtIn.Severity);
    }

    [Fact]
    public void UpdateState_replaces_an_existing_built_in_entry()
    {
        string path = WriteConfiguration("""
            version: 2
            rules:
              - type: builtIn
                code: "SHRD003"
                isActive: true
                severity: "Message"
            """);
        using DiagnosticConfigurationService service = CreateService(path, out _);
        var writer = new DiagnosticConfigurationWriter(service);

        writer.UpdateState(path, "SHRD003", DiagnosticRuleType.BuiltIn,
            isActive: false, severity: DiagnosticSeverity.Warning, isHidden: true);

        DiagnosticConfigurationFile? file = ConfigurationPathUtils.GetDiagnosticConfiguration(path);
        DiagnosticRule rule = Assert.Single(file!.Rules);
        Assert.False(rule.IsActive);
        Assert.Equal(DiagnosticSeverity.Warning, rule.Severity);
        Assert.True(rule.IsHidden);
    }

    [Fact]
    public void UpdateStates_persists_a_batch_of_rules_in_one_file()
    {
        string path = WriteConfiguration("""
            # Keep this comment.
            version: 2
            rules:
              - type: custom
                code: "CSTM001"
                take: "instance"
                visualizations:
                  - name: "Show"
                    steps:
                      - type: "Show"
                        elementSets: ["Target"]
              - type: builtIn
                code: "SHRD003"
                isActive: true
                severity: "Message"
            """);
        using DiagnosticConfigurationService service = CreateService(path, out _);
        var writer = new DiagnosticConfigurationWriter(service);

        writer.UpdateStates(
            path,
            [
                new DiagnosticRuleStateUpdate(
                    "CSTM001", DiagnosticRuleType.Custom, IsActive: false, Severity: DiagnosticSeverity.Warning, IsHidden: false),
                new DiagnosticRuleStateUpdate(
                    "SHRD003", DiagnosticRuleType.BuiltIn, IsActive: false, Severity: DiagnosticSeverity.Warning, IsHidden: true),
            ]);

        string content = File.ReadAllText(path);
        Assert.Contains("# Keep this comment.", content, StringComparison.Ordinal);
        DiagnosticConfigurationFile? file = ConfigurationPathUtils.GetDiagnosticConfiguration(path);
        Assert.NotNull(file);
        Assert.Equal(2, file.Rules.Count);
        Assert.False(file.Rules[0].IsActive);
        Assert.Equal(DiagnosticSeverity.Warning, file.Rules[0].Severity);
        Assert.False(file.Rules[1].IsActive);
        Assert.True(file.Rules[1].IsHidden);
    }

    [Fact]
    public void UpdateStates_appends_several_built_in_overrides_in_one_batch()
    {
        string path = WriteConfiguration("""
            version: 2
            rules:
              - type: custom
                code: "CSTM001"
                take: "instance"
            """);
        using DiagnosticConfigurationService service = CreateService(path, out _);
        var writer = new DiagnosticConfigurationWriter(service);

        writer.UpdateStates(
            path,
            [
                new DiagnosticRuleStateUpdate(
                    "SHRD003", DiagnosticRuleType.BuiltIn, IsActive: false, Severity: DiagnosticSeverity.Warning, IsHidden: false),
                new DiagnosticRuleStateUpdate(
                    "SHRD004", DiagnosticRuleType.BuiltIn, IsActive: false, Severity: DiagnosticSeverity.Warning, IsHidden: false),
            ]);

        string content = File.ReadAllText(path);
        Assert.Equal(
            1,
            content.Split(new[] { "# Built-in overrides" }, StringSplitOptions.None).Length - 1);
        DiagnosticConfigurationFile? file = ConfigurationPathUtils.GetDiagnosticConfiguration(path);
        Assert.NotNull(file);
        Assert.Equal(3, file.Rules.Count);
        Assert.Equal("SHRD003", file.Rules[1].Code);
        Assert.Equal("SHRD004", file.Rules[2].Code);
    }

    [Fact]
    public void UpdateStates_with_an_empty_batch_does_not_create_the_file()
    {
        string path = Path.Combine(_directory, ConfigurationPathUtils.DiagnosticsConfigFileName);
        using DiagnosticConfigurationService service = CreateService(path, out _);
        var writer = new DiagnosticConfigurationWriter(service);

        writer.UpdateStates(path, []);

        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Reset_removes_built_in_entries_and_hidden_fields()
    {
        string path = WriteConfiguration("""
            # Header comment.
            version: 2
            rules:
              - type: custom
                code: "CSTM001"
                takeDocument: "!property('IsFamilyDocument')"
                take: "instance"
                check: "true"
                isHidden: true

              # Built-in overrides
              - type: builtIn
                code: "SHRD003"
                isActive: false
            """);
        using DiagnosticConfigurationService service = CreateService(path, out _);
        var writer = new DiagnosticConfigurationWriter(service);

        writer.ResetBuiltInOverrides();

        string content = File.ReadAllText(path);
        Assert.Contains("# Header comment.", content, StringComparison.Ordinal);
        Assert.DoesNotContain("builtIn", content, StringComparison.Ordinal);
        Assert.DoesNotContain("isHidden", content, StringComparison.Ordinal);
        DiagnosticConfigurationFile? file = ConfigurationPathUtils.GetDiagnosticConfiguration(path);
        DiagnosticRule rule = Assert.Single(file!.Rules);
        Assert.Equal("CSTM001", rule.Code);
    }

    [Fact]
    public void Writer_replaces_an_empty_flow_rule_list()
    {
        string path = WriteConfiguration("""
            version: 2
            rules: []
            """);
        using DiagnosticConfigurationService service = CreateService(path, out _);
        var writer = new DiagnosticConfigurationWriter(service);

        writer.UpdateState(path, "SHRD003", DiagnosticRuleType.BuiltIn,
            isActive: true, severity: DiagnosticSeverity.Message, isHidden: false);

        DiagnosticConfigurationFile? file = ConfigurationPathUtils.GetDiagnosticConfiguration(path);
        DiagnosticRule rule = Assert.Single(file!.Rules);
        Assert.Equal("SHRD003", rule.Code);
    }

    [Fact]
    public void Writer_preserves_the_byte_order_mark()
    {
        string path = Path.Combine(_directory, ConfigurationPathUtils.DiagnosticsConfigFileName);
        File.WriteAllText(path, "version: 2\nrules: []\n", new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using DiagnosticConfigurationService service = CreateService(path, out _);
        var writer = new DiagnosticConfigurationWriter(service);

        writer.UpdateState(path, "SHRD003", DiagnosticRuleType.BuiltIn,
            isActive: true, severity: DiagnosticSeverity.Message, isHidden: false);

        byte[] bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length >= 3);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
    }

    private static DiagnosticConfigurationService CreateService(
        string path, out DiagnosticConfigurationErrorState errors)
    {
        errors = new DiagnosticConfigurationErrorState();
        return new DiagnosticConfigurationService(
            [new DiagnosticConfigurationFileDescriptor(path, IsTopLevel: true)], errors);
    }

    private string WriteConfiguration(string content)
    {
        string path = Path.Combine(_directory, ConfigurationPathUtils.DiagnosticsConfigFileName);
        File.WriteAllText(path, content);
        return path;
    }
}
