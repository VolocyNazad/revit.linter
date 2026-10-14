using Revit.Linter.ConfigurationPath;
using Revit.Linter.ConfigurationPath.Abstractions.Models;
using Revit.Linter.Testing;
using CollisionRule = Revit.Linter.CollisionDiagnostics.Models.DiagnosticRule;
using ParameterRule = Revit.Linter.ParameterElementDiagnostics.Models.DiagnosticRule;
using UserRule = Revit.Linter.UserDiagnostics.Models.DiagnosticRule;

namespace Revit.Linter.UserDiagnostics.Tests;

public sealed class ExampleConfigurationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("ru")]
    public void All_example_configuration_files_deserialize_with_supported_version(string languageFolder)
    {
        ConfigurationFile<UserRule>? userFile = Read<UserRule>(languageFolder, "config.yaml");
        ConfigurationFile<CollisionRule>? collisionFile =
            Read<CollisionRule>(languageFolder, "collision.config.yaml");
        ConfigurationFile<ParameterRule>? parameterFile =
            Read<ParameterRule>(languageFolder, "parameter-element.config.yaml");

        Assert.Equal(ConfigurationPathUtils.SupportedConfigurationVersion, userFile?.Version);
        Assert.NotEmpty(userFile?.Rules ?? []);
        Assert.Equal(ConfigurationPathUtils.SupportedConfigurationVersion, collisionFile?.Version);
        Assert.NotEmpty(collisionFile?.Rules ?? []);
        Assert.Equal(ConfigurationPathUtils.SupportedConfigurationVersion, parameterFile?.Version);
        Assert.NotEmpty(parameterFile?.Rules ?? []);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ru")]
    public void Practical_tour_configuration_deserializes_with_two_visualizations(string languageFolder)
    {
        ConfigurationFile<CollisionRule>? tourFile = Read<CollisionRule>(
            languageFolder, "practical-tour.config.yaml");

        Assert.Equal(ConfigurationPathUtils.SupportedConfigurationVersion, tourFile?.Version);
        CollisionRule tourRule = Assert.Single(tourFile?.Rules ?? [], rule => rule.Code == "TOUR001");
        Assert.Equal(2, tourRule.Visualizations.Length);
    }

    private static ConfigurationFile<TRule>? Read<TRule>(string languageFolder, string fileName)
        where TRule : class =>
        ConfigurationPathUtils.GetVersionedConfigurations<TRule>(Path.Combine(RepositoryRoot.Find(),
            "wiki", "examples", "configuration", languageFolder, fileName));
}
