using Revit.Linter.DiagnosticConfiguration.Models;
using Revit.Linter.ConfigurationPath.Services;
using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.ConfigurationPath.Tests;

public sealed class BuiltInDiagnosticStateProviderTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), nameof(BuiltInDiagnosticStateProviderTests), Guid.NewGuid().ToString("N"));

    public BuiltInDiagnosticStateProviderTests() => Directory.CreateDirectory(_directory);

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
    public void Provider_applies_a_built_in_entry_and_falls_back_to_defaults()
    {
        string path = Path.Combine(_directory, ConfigurationPathUtils.DiagnosticsConfigFileName);
        File.WriteAllText(path, """
            version: 2
            rules:
              - type: builtIn
                code: "SHRD003"
                isActive: false
                severity: "Warning"
                isHidden: true
            """);
        var errors = new DiagnosticConfigurationErrorState();
        using var service = new DiagnosticConfigurationService(
            [new DiagnosticConfigurationFileDescriptor(path, IsTopLevel: true)], errors);
        var provider = new BuiltInDiagnosticStateProvider(service);

        DiagnosticState overridden = provider.GetState(
            "SHRD003", DiagnosticSeverity.Error, defaultIsActive: true);
        DiagnosticState missing = provider.GetState(
            "SHRD001", DiagnosticSeverity.Error, defaultIsActive: false);

        Assert.False(overridden.IsActive);
        Assert.Equal(DiagnosticSeverity.Warning, overridden.Severity);
        Assert.True(overridden.IsHidden);
        Assert.False(missing.IsActive);
        Assert.Equal(DiagnosticSeverity.Error, missing.Severity);
        Assert.False(missing.IsHidden);
        Assert.Equal(["SHRD003"], provider.OverriddenCodes);
    }

    [Fact]
    public void Provider_uses_the_first_matching_entry_by_source_precedence()
    {
        string user = Write("diagnostics.config.yaml", """
            version: 2
            rules:
              - type: builtIn
                code: "SHRD003"
                isActive: true
            """);
        string example = Write(Path.Combine("examples", "diagnostics.config.yaml"), """
            version: 2
            rules:
              - type: builtIn
                code: "SHRD003"
                isActive: false
            """);
        var errors = new DiagnosticConfigurationErrorState();
        using var service = new DiagnosticConfigurationService(
        [
            new DiagnosticConfigurationFileDescriptor(user, IsTopLevel: true),
            new DiagnosticConfigurationFileDescriptor(example, IsExample: true),
        ], errors);
        var provider = new BuiltInDiagnosticStateProvider(service);

        DiagnosticState state = provider.GetState("SHRD003", DiagnosticSeverity.Message, defaultIsActive: false);

        Assert.True(state.IsActive);
    }

    private string Write(string name, string content)
    {
        string path = Path.Combine(_directory, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }
}
