using Revit.Linter.ConfigurationPath;
using Revit.Linter.ConfigurationPath.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.UserDiagnostics.Tests;

public sealed class UserDiagnosticFixRegistrationTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(), nameof(UserDiagnosticFixRegistrationTests), Guid.NewGuid().ToString("N"));

    [Fact]
    public void Configured_yaml_fix_is_deserialized_from_the_shared_file()
    {
        Directory.CreateDirectory(_tempDirectory);
        string path = Path.Combine(_tempDirectory, ConfigurationPathUtils.DiagnosticsConfigFileName);
        File.WriteAllText(path, """
            version: 2
            rules:
              - type: custom
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

        DiagnosticConfigurationFile file = Assert.IsType<DiagnosticConfigurationFile>(
            ConfigurationPathUtils.GetDiagnosticConfiguration(path));
        DiagnosticRule rule = Assert.Single(file.Rules);
        ElementFixPipelineDefinition fix = Assert.Single(rule.Fixes);

        Assert.Equal("Delete target", fix.Name);
        Assert.Equal("Delete", fix.Steps[0].Type);
        Assert.Equal(["Target", "Dependencies"], fix.Steps[0].ElementSets);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory)) Directory.Delete(_tempDirectory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup of a test directory.
        }
    }
}
