using Revit.Linter.ConfigurationPath;
using Revit.Linter.DiagnosticConfiguration.Models;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Testing;

namespace Revit.Linter.UserDiagnostics.Tests;

public sealed class ExampleConfigurationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("ru")]
    public void All_example_rules_are_in_one_version_two_file(string languageFolder)
    {
        DiagnosticConfigurationFile? file = Read(languageFolder, ConfigurationPathUtils.DiagnosticsConfigFileName);

        Assert.NotNull(file);
        Assert.Equal(ConfigurationPathUtils.SupportedConfigurationVersion, file.Version);
        Assert.Equal(6, file.Rules.Count(rule => rule.Type == "custom"));
        Assert.Equal(6, file.Rules.Count(rule => rule.Type == "collision"));
        Assert.Equal(3, file.Rules.Count(rule => rule.Type == "parameter"));
        Assert.DoesNotContain(file.Rules, rule => rule.Type == "builtIn");
    }

    [Theory]
    [InlineData("", "Shared", "Systems", "Architecture", "Structure")]
    [InlineData("ru", "Общие", "Системы", "Архитектура", "Конструкции")]
    public void Every_example_rule_starts_with_the_discipline_tag_of_its_block(
        string languageFolder, string shared, string systems, string architecture, string structure)
    {
        DiagnosticConfigurationFile file = Assert.IsType<DiagnosticConfigurationFile>(
            Read(languageFolder, ConfigurationPathUtils.DiagnosticsConfigFileName));

        foreach (DiagnosticRule rule in file.Rules)
        {
            string code = rule.Code!;
            string expected = ExpectedDiscipline(code, shared, systems, architecture, structure);

            Assert.NotEmpty(rule.Tags);
            Assert.Equal(expected, rule.Tags[0]);
            Assert.DoesNotContain(
                rule.Tags.Skip(1),
                tag => tag == shared || tag == systems || tag == architecture || tag == structure);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("ru")]
    public void Comments_example_rule_declares_a_parameter_fix(string languageFolder)
    {
        DiagnosticConfigurationFile file = Assert.IsType<DiagnosticConfigurationFile>(
            Read(languageFolder, ConfigurationPathUtils.DiagnosticsConfigFileName));
        DiagnosticRule rule = Assert.Single(file.Rules, candidate => candidate.Code == "CSTM001");
        ElementFixPipelineDefinition fix = Assert.Single(rule.Fixes);
        ElementFixStepDefinition step = Assert.Single(fix.Steps);

        Assert.False(string.IsNullOrWhiteSpace(fix.Name));
        Assert.Equal("SetParameter", step.Type);
        Assert.Equal("ALL_MODEL_INSTANCE_COMMENTS", step.Parameter);
        Assert.False(string.IsNullOrWhiteSpace(step.Value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ru")]
    public void Practical_tour_configuration_uses_the_single_file_format(string languageFolder)
    {
        DiagnosticConfigurationFile? tourFile = Read(languageFolder, ConfigurationPathUtils.PracticalTourFileName);

        Assert.NotNull(tourFile);
        Assert.Equal(ConfigurationPathUtils.SupportedConfigurationVersion, tourFile.Version);
        DiagnosticRule tourRule = Assert.Single(tourFile.Rules, rule => rule.Code == "TOUR001");
        Assert.Equal("collision", tourRule.Type);
        Assert.False(tourRule.IsActive);
        Assert.Equal(3, tourRule.Visualizations.Length);
    }

    private static string ExpectedDiscipline(
        string code, string shared, string systems, string architecture, string structure) =>
        code switch
        {
            "CSTM001" => shared,
            _ when code.StartsWith("CSTM1", StringComparison.Ordinal) ||
                   code.StartsWith("CLSN1", StringComparison.Ordinal) ||
                   code.StartsWith("PRMTR1", StringComparison.Ordinal) => systems,
            _ when code.StartsWith("CSTM2", StringComparison.Ordinal) ||
                   code.StartsWith("CLSN2", StringComparison.Ordinal) ||
                   code.StartsWith("PRMTR2", StringComparison.Ordinal) => architecture,
            _ when code.StartsWith("CLSN3", StringComparison.Ordinal) ||
                   code.StartsWith("PRMTR3", StringComparison.Ordinal) => structure,
            _ => throw new InvalidOperationException($"Unexpected example rule code '{code}'."),
        };

    private static DiagnosticConfigurationFile? Read(string languageFolder, string fileName) =>
        ConfigurationPathUtils.GetDiagnosticConfiguration(Path.Combine(
            RepositoryRoot.Find(), "wiki", "examples", "configuration", languageFolder, fileName));
}