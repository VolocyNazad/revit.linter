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
        Assert.Equal(11, file.Rules.Count(rule => rule.Type == "collision"));
        Assert.Equal(3, file.Rules.Count(rule => rule.Type == "parameter"));
        Assert.DoesNotContain(file.Rules, rule => rule.Type == "builtIn");
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

    private static DiagnosticConfigurationFile? Read(string languageFolder, string fileName) =>
        ConfigurationPathUtils.GetDiagnosticConfiguration(Path.Combine(
            RepositoryRoot.Find(), "wiki", "examples", "configuration", languageFolder, fileName));
}