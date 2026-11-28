using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.TypeCatalogDiagnostics.DI;

namespace Revit.Linter.TypeCatalogDiagnostics.RevitTests;

public sealed class TypeCatalogDiagnosticsRegistrationTests
{
    [Test]
    public async Task Provider_registers_every_type_catalog_diagnostic()
    {
        var services = new ServiceCollection();
        services.AddTypeCatalogDiagnostics();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IBuiltInDiagnosticStateProvider>(new TestBuiltInStateProvider());
        services.AddSingleton<IElementVisualizationPipelineFactory, TestElementVisualizationPipelineFactory>();
        using ServiceProvider provider = services.BuildServiceProvider();

        string[] codes = provider.GetServices<IDiagnosticRegistrationProvider>()
            .SelectMany(registrationProvider => registrationProvider.GetElementDiagnostics())
            .Select(registration => registration.Identity.Code)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

        await Assert.That(codes).IsEquivalentTo(["SHRD0046", "SHRD0047", "SHRD0048", "SHRD0049"]);
    }

    private sealed class TestBuiltInStateProvider : IBuiltInDiagnosticStateProvider
    {
        public string ConfigurationPath => string.Empty;

        public IReadOnlyList<string> OverriddenCodes => [];

        public DiagnosticState GetState(
            string code, DiagnosticSeverity defaultSeverity, bool defaultIsActive) =>
            new(defaultIsActive, defaultSeverity, false);
    }

    private sealed class TestElementVisualizationPipelineFactory : IElementVisualizationPipelineFactory
    {
        public IElementVisualizationPipeline Create(
            ElementDiagnosticId identity,
            string name,
            IReadOnlyList<ElementVisualizationStepDefinition> steps) =>
            new TestElementVisualizationPipeline(identity, name);
    }

    private sealed class TestElementVisualizationPipeline(ElementDiagnosticId identity, string value)
        : IElementVisualizationPipeline
    {
        public ElementDiagnosticId Identity { get; } = identity;
        public string Value { get; } = value;
        public bool IsRestored { get; private set; }
        public bool Apply(ElementVisualizationContext context) => true;
        public void Restore() => IsRestored = true;
        public void Dispose() => Restore();
    }
}
