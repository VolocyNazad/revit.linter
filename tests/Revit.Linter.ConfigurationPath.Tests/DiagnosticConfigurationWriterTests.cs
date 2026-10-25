using Revit.Linter.ConfigurationPath.Abstractions.Models;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
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
