using Revit.Linter.ConfigurationPath;
using Revit.Linter.Testing;
using CollisionRule = Revit.Linter.CollisionDiagnostics.Models.DiagnosticRule;
using ParameterRule = Revit.Linter.ParameterElementDiagnostics.Models.DiagnosticRule;
using UserRule = Revit.Linter.UserDiagnostics.Models.DiagnosticRule;

namespace Revit.Linter.UserDiagnostics.Tests;

public sealed class ExampleConfigurationTests
{
    [Fact]
    public void All_example_configuration_files_deserialize()
    {
        List<UserRule>? userRules = Read<List<UserRule>>("config.yaml");
        List<CollisionRule>? collisionRules = Read<List<CollisionRule>>("collision.config.yaml");
        List<ParameterRule>? parameterRules = Read<List<ParameterRule>>("parameter-element.config.yaml");

        Assert.NotNull(userRules);
        Assert.NotEmpty(userRules);
        Assert.NotNull(collisionRules);
        Assert.NotEmpty(collisionRules);
        Assert.NotNull(parameterRules);
        Assert.NotEmpty(parameterRules);
    }

    private static T? Read<T>(string fileName) where T : class =>
        ConfigurationPathUtils.GetConfigurations<T>(Path.Combine(RepositoryRoot.Find(),
            "wiki", "examples", "configuration", fileName));
}
