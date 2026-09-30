using Revit.Linter.ConfigurationPath;
using Revit.Linter.UserDiagnostics.Models;

namespace Revit.Linter.UserDiagnostics.Tests;

public sealed class UserDiagnosticFixRegistrationTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(), nameof(UserDiagnosticFixRegistrationTests), Guid.NewGuid().ToString("N"));

    [Fact]
    public void Configured_yaml_fix_is_created_for_registration()
    {
        Directory.CreateDirectory(_tempDirectory);
        string path = Path.Combine(_tempDirectory, "config.yaml");
        File.WriteAllText(path, """
            code: TEST
            takeDocument: "true"
            take: "instance"
            check: "true"
            description: Test
            message: Test
            fixes:
              - name: Delete target
                steps:
                  - type: Delete
                    elementSets: [Target, Dependencies]
            """);
        DiagnosticRule rule = ConfigurationPathUtils.GetConfigurations<DiagnosticRule>(path)!;
        (string Name, string StepType, string[] ElementSets)[] fixes =
            UserDiagnosticRegistrationProvider.CreateFixes(
                rule, (name, steps) => (name, steps.Single().Type, steps.Single().ElementSets));

        Assert.Single(fixes);
        Assert.Equal("Delete target", fixes[0].Name);
        Assert.Equal("Delete", fixes[0].StepType);
        Assert.Equal(["Target", "Dependencies"], fixes[0].ElementSets);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory)) Directory.Delete(_tempDirectory, recursive: true);
    }
}
