using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.ConfigurationPath.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.ElementAccentor.DI;
using Revit.Linter.ElementFixing.DI;
using Revit.Linter.ElementVisualization.DI;
using Revit.Linter.Testing;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class ExamplePipelineTests
{
    [Test]
    public async Task All_example_fix_and_visualization_pipelines_build()
    {
        using ServiceProvider provider = CreateProvider();
        IElementFixPipelineFactory fixFactory = provider.GetRequiredService<IElementFixPipelineFactory>();
        IElementVisualizationPipelineFactory visualizationFactory =
            provider.GetRequiredService<IElementVisualizationPipelineFactory>();
        int builtCount = 0;

        foreach (DiagnosticRule rule in Read(ConfigurationPathUtils.DiagnosticsConfigFileName))
        {
            if (rule.Type is not ("custom" or "collision")) continue;
            builtCount += BuildRule(rule, fixFactory, visualizationFactory);
        }

        // The managed practical-tour configuration is a collision rule as well.
        foreach (DiagnosticRule rule in Read(ConfigurationPathUtils.PracticalTourFileName))
            builtCount += BuildRule(rule, fixFactory, visualizationFactory);

        await Assert.That(builtCount).IsGreaterThan(0);
    }

    private static int BuildRule(
        DiagnosticRule rule,
        IElementFixPipelineFactory fixFactory,
        IElementVisualizationPipelineFactory visualizationFactory)
    {
        ElementDiagnosticId identity = CreateIdentity(rule.Code!, rule.Description ?? string.Empty);
        int builtCount = 0;
        foreach (ElementFixPipelineDefinition fix in rule.Fixes)
        {
            fixFactory.Create(identity, fix.Name, fix.Steps);
            builtCount++;
        }

        foreach (ElementVisualizationPipelineDefinition visualization in rule.Visualizations)
        {
            visualizationFactory.Create(identity, visualization.Name, visualization.Steps);
            builtCount++;
        }

        return builtCount;
    }

    private static ElementDiagnosticId CreateIdentity(string code, string description) => new(
        code, description, "Message", DiagnosticSeverity.Message, true, false, string.Empty);

    private static ServiceProvider CreateProvider()
    {
        ServiceCollection services = new();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
            .AddElementAccentor()
            .AddElementFixing()
            .AddElementVisualization();
        return services.BuildServiceProvider();
    }

    private static IReadOnlyList<DiagnosticRule> Read(string fileName) =>
        ConfigurationPathUtils.GetDiagnosticConfiguration(Path.Combine(
            RepositoryRoot.Find(), "wiki", "examples", "configuration", fileName))?.Rules ?? [];
}
