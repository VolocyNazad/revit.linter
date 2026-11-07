using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.DiagnosticConfiguration.Editing;
using Revit.Linter.DiagnosticConfiguration.Models;
using Revit.Linter.DiagnosticConfiguration.Parsing;
using Revit.Linter.DiagnosticConfiguration.Validation;
using Revit.Linter.Testing;

namespace Revit.Linter.DiagnosticConfiguration.Tests;

/// <summary>
/// Edits the shipped example configuration the way the add-in and the diagnostics editor do, and checks
/// that every rule still parses, validates and keeps the hand-written documentation around it.
/// </summary>
public sealed class ExampleConfigurationTests
{
    private static string ExamplePath => Path.Combine(
        RepositoryRoot.Find(),
        "wiki",
        "examples",
        "configuration",
        DiagnosticConfigurationFormat.FileName);

    [Fact]
    public void The_shipped_example_parses_and_every_rule_validates()
    {
        DiagnosticConfigurationFile file = ReadExample();

        Assert.Equal(DiagnosticConfigurationFormat.SupportedVersion, file.Version);
        Assert.NotEmpty(file.Rules);
        foreach (DiagnosticRule rule in file.Rules)
            Assert.Empty(DiagnosticRuleValidator.Validate(rule));
    }

    [Fact]
    public void A_state_edit_keeps_comments_and_every_rule()
    {
        string original = File.ReadAllText(ExamplePath);
        DiagnosticConfigurationFile before = DiagnosticConfigurationReader.Read(original)!;
        DiagnosticRule first = before.Rules[0];
        var location = new DiagnosticRuleLocation(
            DiagnosticRuleTypes.TryParse(first.Type, out DiagnosticRuleType type) ? type : DiagnosticRuleType.Custom,
            first.Code!);

        string updated = DiagnosticConfigurationEditor.Parse(original)
            .SetScalar(location, "severity", "\"Error\"");

        Assert.Contains("# >>> section: mep", updated, StringComparison.Ordinal);
        Assert.Contains("# <<< section: mep", updated, StringComparison.Ordinal);
        Assert.Equal(before.Rules.Count, DiagnosticConfigurationReader.Read(updated)!.Rules.Count);
        DiagnosticRule changed = DiagnosticConfigurationReader.Read(updated)!.Rules[0];
        Assert.Equal(DiagnosticSeverity.Error, changed.Severity);
        Assert.Equal(first.Code, changed.Code);
        Assert.Equal(first.Take, changed.Take);
    }

    [Fact]
    public void Appending_a_rule_keeps_the_existing_document_intact()
    {
        string original = File.ReadAllText(ExamplePath);
        DiagnosticConfigurationFile before = DiagnosticConfigurationReader.Read(original)!;

        string updated = DiagnosticConfigurationEditor.Parse(original).AppendRule(
            "- type: builtIn\n  code: \"SHRD003\"\n  isActive: false\n  severity: \"Warning\"",
            "# Built-in overrides");

        Assert.Contains("# >>> section: mep", updated, StringComparison.Ordinal);
        DiagnosticConfigurationFile after = DiagnosticConfigurationReader.Read(updated)!;
        Assert.Equal(before.Rules.Count + 1, after.Rules.Count);
        Assert.Equal("SHRD003", after.Rules[after.Rules.Count - 1].Code);
    }

    [Fact]
    public void A_category_visibility_step_parses_with_its_categories_and_mode()
    {
        const string yaml = """
            version: 2
            rules:
              - type: custom
                code: "CSTM001"
                take: "instance"
                check: "true"
                visualizations:
                  - name: "Hide"
                    steps:
                      - type: "SetCategoryVisibility"
                        categories: ["OST_VolumeOfInterest", "ost_grids"]
                        mode: "hide"
            """;

        DiagnosticConfigurationFile file = DiagnosticConfigurationReader.Read(yaml)!;
        ElementVisualizationStepDefinition step = file.Rules[0].Visualizations[0].Steps[0];

        Assert.Equal("SetCategoryVisibility", step.Type);
        Assert.Equal(["OST_VolumeOfInterest", "ost_grids"], step.Categories);
        Assert.Equal("hide", step.Mode);
    }

    private static DiagnosticConfigurationFile ReadExample() =>
        DiagnosticConfigurationReader.Read(File.ReadAllText(ExamplePath))
        ?? throw new InvalidOperationException("The example configuration is empty.");
}
