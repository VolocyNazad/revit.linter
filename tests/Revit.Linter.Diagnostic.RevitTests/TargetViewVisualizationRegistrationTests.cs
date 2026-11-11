using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.ElementAccentor.DI;
using Revit.Linter.ElementDiagnostics;
using Revit.Linter.Testing;

namespace Revit.Linter.Diagnostic.RevitTests;

/// <summary>
/// Guards the product rule that a built-in finding whose target is a view is visualized by opening
/// that view: every built-in diagnostic offers the target-view pipeline, and no other module does.
/// </summary>
public sealed class TargetViewVisualizationRegistrationTests : DiagnosticTestFixture
{
    [Test]
    public async Task Every_built_in_diagnostic_offers_the_target_view_pipeline()
    {
        ServiceCollection services = new();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(TestDocumentQueries.Create());
        services.AddElementAccentor();
        await using ServiceProvider provider = services.BuildServiceProvider();
        ElementDiagnosticRegistrationProvider registrationProvider = new(
            provider,
            new FallbackPipelineFactory(),
            new TestBuiltInStateProvider());

        ElementDiagnosticRegistration[] registrations =
            registrationProvider.GetElementDiagnostics().ToArray();
        string expectedName = ElementDiagnosticLocalizations.GetString("open_view_visualization");

        await Assert.That(registrations.Length).IsGreaterThan(0);
        foreach (ElementDiagnosticRegistration registration in registrations)
        {
            ITargetViewVisualizationPipeline[] pipelines = registration.VisualizationPipelines
                .OfType<ITargetViewVisualizationPipeline>()
                .ToArray();

            await Assert.That(pipelines.Length).IsEqualTo(1);
            await Assert.That(pipelines[0].Identity.Code).IsEqualTo(registration.Identity.Code);
            await Assert.That(pipelines[0].Value).IsEqualTo(expectedName);
        }
    }

    private sealed class FallbackPipelineFactory : IElementVisualizationPipelineFactory
    {
        public IElementVisualizationPipeline Create(
            ElementDiagnosticId identity,
            string name,
            IReadOnlyList<ElementVisualizationStepDefinition> steps) => new FallbackPipeline(identity, name);
    }

    private sealed class FallbackPipeline(ElementDiagnosticId identity, string value)
        : IElementVisualizationPipeline
    {
        public ElementDiagnosticId Identity { get; } = identity;
        public string Value { get; } = value;
        public bool Apply(ElementVisualizationContext context) => true;
        public void Restore() { }
        public void Dispose() { }
    }
}
