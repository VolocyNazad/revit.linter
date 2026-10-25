using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.DiagnosticConfiguration.Editing;
using Revit.Linter.DiagnosticConfiguration.Parsing;

namespace Revit.Linter.DiagnosticConfiguration.Tests;

public sealed class DiagnosticConfigurationEditorTests
{
    private static readonly DiagnosticRuleLocation CustomRule = new(DiagnosticRuleType.Custom, "CSTM001");

    [Fact]
    public void SetScalar_replaces_only_the_value_and_keeps_comments()
    {
        const string text = """
            # Header comment.
            version: 2
            rules:
              - type: custom
                code: "CSTM001"
                # The state comment must survive.
                isActive: true
                take: "instance"
            """;

        string updated = DiagnosticConfigurationEditor.Parse(text)
            .SetScalar(CustomRule, "isActive", "false");

        Assert.Contains("# Header comment.", updated, StringComparison.Ordinal);
        Assert.Contains("# The state comment must survive.", updated, StringComparison.Ordinal);
        Assert.Contains("isActive: false", updated, StringComparison.Ordinal);
        Assert.DoesNotContain("isActive: true", updated, StringComparison.Ordinal);
        Assert.Equal(false, Assert.Single(DiagnosticConfigurationReader.Read(updated)!.Rules).IsActive);
    }

    [Fact]
    public void SetScalar_inserts_a_missing_field_with_the_rule_indentation()
    {
        const string text = """
            version: 2
            rules:
              - type: custom
                code: "CSTM001"
                take: "instance"
            """;

        string updated = DiagnosticConfigurationEditor.Parse(text)
            .SetScalar(CustomRule, "severity", "\"Warning\"");

        Assert.Contains("\n    severity: \"Warning\"", updated, StringComparison.Ordinal);
        Assert.Equal(DiagnosticSeverity.Warning, Assert.Single(DiagnosticConfigurationReader.Read(updated)!.Rules).Severity);
    }

    [Fact]
    public void SetScalar_handles_a_rule_list_without_item_indentation()
    {
        const string text = """
            version: 2
            rules:
            - type: custom
              code: "CSTM001"
            """;

        string updated = DiagnosticConfigurationEditor.Parse(text)
            .SetScalar(CustomRule, "isHidden", "true");

        Assert.Contains("\n  isHidden: true", updated, StringComparison.Ordinal);
        Assert.True(Assert.Single(DiagnosticConfigurationReader.Read(updated)!.Rules).IsHidden);
    }

    [Fact]
    public void SetScalars_inserts_all_missing_fields_in_the_requested_order()
    {
        const string text = """
            version: 2
            rules:
              - type: builtIn
                code: "SHRD003"
            """;

        string updated = DiagnosticConfigurationEditor.Parse(text).SetScalars(
            new DiagnosticRuleLocation(DiagnosticRuleType.BuiltIn, "SHRD003"),
            [
                new("isActive", "false"),
                new("severity", "\"Warning\""),
            ]);

        int active = updated.IndexOf("isActive: false", StringComparison.Ordinal);
        int severity = updated.IndexOf("severity: \"Warning\"", StringComparison.Ordinal);
        Assert.True(active > 0);
        Assert.True(severity > active);
    }

    [Fact]
    public void SetScalars_inserts_missing_fields_after_a_nested_block()
    {
        const string text = """
            version: 2
            rules:
              - type: parameter
                code: "PAR001"
                take: "instance"
                parameters:
                  - name: "P1"
                    isInstance: true
              - type: builtIn
                code: "SHRD003"
            """;

        string updated = DiagnosticConfigurationEditor.Parse(text).SetScalars(
            new DiagnosticRuleLocation(DiagnosticRuleType.Parameter, "PAR001"),
            [
                new("severity", "\"Warning\""),
                new("isActive", "false"),
            ]);

        // A nested block reports an end mark at its first child; the inserted fields must follow the
        // whole block instead of landing inside it, and the next rule must stay intact.
        int block = updated.IndexOf("isInstance: true", StringComparison.Ordinal);
        int severity = updated.IndexOf("severity: \"Warning\"", StringComparison.Ordinal);
        int active = updated.IndexOf("isActive: false", StringComparison.Ordinal);
        Assert.True(block >= 0);
        Assert.True(severity > block);
        Assert.True(active > severity);
        List<DiagnosticRule> rules = DiagnosticConfigurationReader.Read(updated)!.Rules;
        Assert.Equal(2, rules.Count);
        Assert.Equal(DiagnosticSeverity.Warning, rules[0].Severity);
        Assert.False(rules[0].IsActive);
        Assert.Single(rules[0].Parameters!);
        Assert.Equal("SHRD003", rules[1].Code);
    }

    [Fact]
    public void SetScalars_inserts_missing_fields_after_a_flow_sequence()
    {
        const string text = """
            version: 2
            rules:
              - type: custom
                code: "CSTM001"
                description: "Generic models"
                severity: "Message"
                visualizations:
                  - name: "Show and select"
                    steps:
                      - type: "Show"
                        elementSets: ["Target"]
              - type: builtIn
                code: "SHRD003"
            """;

        string updated = DiagnosticConfigurationEditor.Parse(text).SetScalars(
            CustomRule,
            [
                new("isActive", "false"),
                new("isHidden", "false"),
            ]);

        // The flow sequence must keep its closing bracket on the same line; the inserted fields follow
        // the whole rule block instead of landing inside the sequence and corrupting the YAML.
        int closing = updated.IndexOf("\"Target\"]", StringComparison.Ordinal);
        int active = updated.IndexOf("isActive: false", StringComparison.Ordinal);
        int hidden = updated.IndexOf("isHidden: false", StringComparison.Ordinal);
        Assert.True(closing >= 0);
        Assert.True(active > closing);
        Assert.True(hidden > active);
        List<DiagnosticRule> rules = DiagnosticConfigurationReader.Read(updated)!.Rules;
        Assert.Equal(2, rules.Count);
        Assert.False(rules[0].IsActive);
        Assert.Equal(false, rules[0].IsHidden);
        Assert.Equal("SHRD003", rules[1].Code);
    }

    [Fact]
    public void SetScalars_updates_several_rules_in_one_pass()
    {
        const string text = """
            version: 2
            rules:
              - type: custom
                code: "CSTM001"
                take: "instance"
              - type: builtIn
                code: "SHRD003"
                isActive: true
                severity: "Message"
            """;

        string updated = DiagnosticConfigurationEditor.Parse(text).SetScalars(
        [
            new DiagnosticRuleScalarUpdate(
                CustomRule,
                [new("isActive", "false"), new("isHidden", "false")]),
            new DiagnosticRuleScalarUpdate(
                new DiagnosticRuleLocation(DiagnosticRuleType.BuiltIn, "SHRD003"),
                [new("isActive", "false")]),
        ]);

        List<DiagnosticRule> rules = DiagnosticConfigurationReader.Read(updated)!.Rules;
        Assert.Equal(2, rules.Count);
        Assert.False(rules[0].IsActive);
        Assert.Equal(false, rules[0].IsHidden);
        Assert.False(rules[1].IsActive);
        Assert.Equal(DiagnosticSeverity.Message, rules[1].Severity);
    }

    [Fact]
    public void SetScalars_rejects_the_same_rule_twice()
    {
        const string text = """
            version: 2
            rules:
              - type: custom
                code: "CSTM001"
            """;

        Assert.Throws<ArgumentException>(() => DiagnosticConfigurationEditor.Parse(text).SetScalars(
        [
            new DiagnosticRuleScalarUpdate(CustomRule, [new("isActive", "false")]),
            new DiagnosticRuleScalarUpdate(CustomRule, [new("isHidden", "true")]),
        ]));
    }

    [Fact]
    public void RemoveField_removes_the_line_and_keeps_the_other_fields()
    {
        const string text = """
            version: 2
            rules:
              - type: custom
                code: "CSTM001"
                isHidden: true
                take: "instance"
            """;

        string updated = DiagnosticConfigurationEditor.Parse(text).RemoveField(CustomRule, "isHidden");

        Assert.DoesNotContain("isHidden", updated, StringComparison.Ordinal);
        Assert.Contains("take: \"instance\"", updated, StringComparison.Ordinal);
        Assert.Null(Assert.Single(DiagnosticConfigurationReader.Read(updated)!.Rules).IsHidden);
    }

    [Fact]
    public void RemoveField_is_a_no_op_for_an_unknown_rule()
    {
        const string text = "version: 2\nrules: []\n";

        string updated = DiagnosticConfigurationEditor
            .Parse(text)
            .RemoveField(new DiagnosticRuleLocation(DiagnosticRuleType.Custom, "NOPE"), "isHidden");

        Assert.Equal(text, updated);
    }

    [Fact]
    public void RemoveRule_removes_the_rule_and_keeps_the_header_comment()
    {
        const string text = """
            # Header comment.
            version: 2
            rules:
              - type: custom
                code: "CSTM001"
                take: "instance"

              # Built-in overrides
              - type: builtIn
                code: "SHRD003"
                isActive: false
            """;

        string updated = DiagnosticConfigurationEditor.Parse(text)
            .RemoveRule(new DiagnosticRuleLocation(DiagnosticRuleType.BuiltIn, "SHRD003"));

        Assert.Contains("# Header comment.", updated, StringComparison.Ordinal);
        Assert.DoesNotContain("SHRD003", updated, StringComparison.Ordinal);
        Assert.Single(DiagnosticConfigurationReader.Read(updated)!.Rules);
    }

    [Fact]
    public void AppendRule_uses_the_existing_indentation_and_writes_the_marker_once()
    {
        string text = """
            version: 2
            rules:
              - type: custom
                code: "CSTM001"
            """.Replace("\r\n", "\n").Replace("\r", "\n");

        string updated = DiagnosticConfigurationEditor.Parse(text).AppendRule(
            "- type: builtIn\n  code: \"SHRD003\"\n  isActive: false",
            "# Built-in overrides");

        Assert.Contains("# Built-in overrides", updated, StringComparison.Ordinal);
        Assert.Contains("\n  - type: builtIn\n    code: \"SHRD003\"\n    isActive: false\n", updated, StringComparison.Ordinal);
        Assert.Equal(2, DiagnosticConfigurationReader.Read(updated)!.Rules.Count);

        string twice = DiagnosticConfigurationEditor.Parse(updated).AppendRule(
            "- type: builtIn\n  code: \"SHRD004\"", "# Built-in overrides");
        Assert.Equal(
            1,
            twice.Split(new[] { "# Built-in overrides" }, StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void AppendRule_replaces_an_empty_flow_rule_list()
    {
        const string text = "version: 2\nrules: []\n";

        string updated = DiagnosticConfigurationEditor.Parse(text)
            .AppendRule("- type: builtIn\n  code: \"SHRD003\"", "# Built-in overrides");

        Assert.DoesNotContain("rules: []", updated, StringComparison.Ordinal);
        Assert.Equal("SHRD003", Assert.Single(DiagnosticConfigurationReader.Read(updated)!.Rules).Code);
    }

    [Fact]
    public void Editor_preserves_crlf_newlines()
    {
        const string text = "version: 2\r\nrules:\r\n  - type: custom\r\n    code: \"CSTM001\"\r\n";

        string updated = DiagnosticConfigurationEditor.Parse(text)
            .SetScalar(CustomRule, "isHidden", "true");

        Assert.Contains("\r\n    isHidden: true\r\n", updated, StringComparison.Ordinal);
        Assert.DoesNotContain("\n", updated.Replace("\r\n", string.Empty), StringComparison.Ordinal);
    }

    [Fact]
    public void SetScalar_reports_a_missing_rule()
    {
        DiagnosticConfigurationEditor editor = DiagnosticConfigurationEditor.Parse("version: 2\nrules: []\n");

        Assert.Throws<InvalidOperationException>(
            () => editor.SetScalar(CustomRule, "isActive", "false"));
    }
}
