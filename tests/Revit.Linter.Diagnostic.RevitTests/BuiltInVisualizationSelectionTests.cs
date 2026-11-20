using Autodesk.Revit.DB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementAccentor.DI;
using Revit.Linter.ElementDiagnostics;
using Revit.Linter.ElementVisualization.Abstractions.Models;
using Revit.Linter.Testing;

namespace Revit.Linter.Diagnostic.RevitTests;

/// <summary>
/// Guards the product rule that a built-in finding selects only its target, while the other steps keep
/// consuming every element set the finding provides.
/// </summary>
public sealed class BuiltInVisualizationSelectionTests : DiagnosticTestFixture
{
    [Test]
    public async Task Built_in_pipeline_steps_select_only_the_target()
    {
        IReadOnlyList<ElementVisualizationStep>[] pipelines =
        [
            new TestSelectOnly().ExposedSteps,
            new TestShowAndSelect().ExposedSteps,
            new TestCutShowAndSelect().ExposedSteps
        ];

        foreach (IReadOnlyList<ElementVisualizationStep> steps in pipelines)
        {
            AccentElementsStep[] accentSteps = steps.OfType<AccentElementsStep>().ToArray();
            AccentElementsStep select = accentSteps.Single(
                step => step.Type == AccentElementsType.SelectElements);

            await Assert.That(select.ElementSetKeys.SequenceEqual(
                [ElementVisualizationSetKeys.Target])).IsTrue();

            foreach (AccentElementsStep other in accentSteps.Where(
                step => step.Type != AccentElementsType.SelectElements))
                await Assert.That(other.ElementSetKeys).IsEmpty();
        }
    }

    [Test]
    public async Task Fallback_pipeline_selects_only_the_target_and_shows_every_set()
    {
        ServiceCollection services = new();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(TestDocumentQueries.Create());
        services.AddElementAccentor();
        await using ServiceProvider provider = services.BuildServiceProvider();

        CapturingPipelineFactory factory = new();
        ElementDiagnosticRegistrationProvider registrationProvider = new(
            provider, factory, new TestBuiltInStateProvider());
        _ = registrationProvider.GetElementDiagnostics().ToArray();

        CapturedPipeline fallback = factory.Captured.Single(
            pipeline => pipeline.Identity.Code == "SHRD0031");
        ElementVisualizationStepDefinition select = fallback.Steps.Single(step => step.Type == "Select");
        ElementVisualizationStepDefinition show = fallback.Steps.Single(step => step.Type == "Show");

        await Assert.That(select.ElementSets.SequenceEqual(
            [ElementVisualizationSetKeys.Target])).IsTrue();
        await Assert.That(show.ElementSets).IsEmpty();
    }

    private static ElementDiagnosticId CreateIdentity() => new(
        "TEST", "Test", "Test", DiagnosticSeverity.Message, true, false, string.Empty);

    private sealed class TestSelectOnly() : SelectOnlyVisualizationPipeline(
        CreateIdentity(), "Select", [], new UnusedGraphicsService(), new UnusedFilterService(),
        new UnusedCategoriesService(), new UnusedViewSettingsService(), NullLogger.Instance)
    {
        public IReadOnlyList<ElementVisualizationStep> ExposedSteps => Steps;
    }

    private sealed class TestShowAndSelect() : ShowAndSelectVisualizationPipeline(
        CreateIdentity(), "Show and select", [], new UnusedGraphicsService(), new UnusedFilterService(),
        new UnusedCategoriesService(), new UnusedViewSettingsService(), NullLogger.Instance)
    {
        public IReadOnlyList<ElementVisualizationStep> ExposedSteps => Steps;
    }

    private sealed class TestCutShowAndSelect() : CutShowAndSelectVisualizationPipeline(
        CreateIdentity(), "Cut, show and select", [], new UnusedGraphicsService(),
        new UnusedFilterService(), new UnusedCategoriesService(), new UnusedViewSettingsService(),
        NullLogger.Instance)
    {
        public IReadOnlyList<ElementVisualizationStep> ExposedSteps => Steps;
    }

    private sealed class UnusedGraphicsService : IOverrideElementGraphicsService
    {
        public IRestoreSession Apply(
            Document document,
            View view,
            IReadOnlyCollection<ElementId> elementIds,
            ViewGraphicsStyle style) =>
            throw new InvalidOperationException("The test reads pipeline steps only.");
    }

    private sealed class UnusedFilterService : IOverrideFilterGraphicsService
    {
        public IRestoreSession Apply(
            Document document,
            View view,
            IReadOnlyCollection<ElementId> elementIds,
            ViewGraphicsStyle style) =>
            throw new InvalidOperationException("The test reads pipeline steps only.");
    }

    private sealed class UnusedCategoriesService : ISetCategoryVisibilityService
    {
        public IRestoreSession Apply(
            Document document,
            View view,
            IReadOnlyList<BuiltInCategory> categories,
            CategoryVisibilityMode mode) =>
            throw new InvalidOperationException("The test reads pipeline steps only.");
    }

    private sealed class UnusedViewSettingsService : IViewSettingsService
    {
        public IRestoreSession ApplyDetailLevel(Document document, View view, ViewDetailLevel level) =>
            throw new InvalidOperationException("The test reads pipeline steps only.");

        public IRestoreSession ApplyDisplayStyle(Document document, View view, DisplayStyle style) =>
            throw new InvalidOperationException("The test reads pipeline steps only.");
    }

    private sealed record CapturedPipeline(
        ElementDiagnosticId Identity,
        string Name,
        IReadOnlyList<ElementVisualizationStepDefinition> Steps);

    private sealed class CapturingPipelineFactory : IElementVisualizationPipelineFactory
    {
        public List<CapturedPipeline> Captured { get; } = [];

        public IElementVisualizationPipeline Create(
            ElementDiagnosticId identity,
            string name,
            IReadOnlyList<ElementVisualizationStepDefinition> steps)
        {
            Captured.Add(new(identity, name, steps));
            return new UnusedPipeline(identity, name);
        }
    }

    private sealed class UnusedPipeline(ElementDiagnosticId identity, string value)
        : IElementVisualizationPipeline
    {
        public ElementDiagnosticId Identity { get; } = identity;
        public string Value { get; } = value;
        public bool Apply(ElementVisualizationContext context) => true;
        public void Restore() { }
        public void Dispose() { }
    }
}
