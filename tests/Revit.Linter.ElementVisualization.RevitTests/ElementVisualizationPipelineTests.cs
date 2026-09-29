using Autodesk.Revit.DB;
using Microsoft.Extensions.Logging.Abstractions;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementVisualization.Abstractions.Models;
using Revit.Linter.ElementVisualization.Abstractions.Services;
using TUnit.Core.Executors;

namespace Revit.Linter.ElementVisualization.RevitTests;

public sealed class ElementVisualizationPipelineTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Restore_runs_completed_steps_in_reverse_order_once()
    {
        RecordingGraphicsService graphicsService = new();
        using TestPipeline pipeline = CreatePipeline(graphicsService);

        bool result = pipeline.Apply(CreateContext());
        pipeline.Restore();
        pipeline.Restore();

        await Assert.That(result).IsTrue();
        await Assert.That(graphicsService.Events.SequenceEqual(
            ["apply:First", "apply:Second", "restore:Second", "restore:First"])).IsTrue();
    }

    [Test]
    public async Task Apply_failure_restores_already_completed_steps()
    {
        RecordingGraphicsService graphicsService = new(throwOnApply: 2);
        using TestPipeline pipeline = CreatePipeline(graphicsService);

        Exception? exception = CaptureException(() => pipeline.Apply(CreateContext()));
        pipeline.Restore();

        await Assert.That(exception).IsTypeOf<InvalidOperationException>();
        await Assert.That(graphicsService.Events.SequenceEqual(
            ["apply:First", "apply:Second", "restore:First"])).IsTrue();
    }

    [Test]
    public async Task Accent_step_uses_registered_service_and_restores_it()
    {
        RecordingAccentService accentService = new();
        using TestPipeline pipeline = new(
            CreateIdentity(),
            new RecordingGraphicsService(),
            [new AccentElementsStep(AccentElementsType.IsolateElementsOnView, ["First"])],
            [accentService]);

        bool result = pipeline.Apply(CreateContext());
        pipeline.Restore();

        await Assert.That(result).IsTrue();
        await Assert.That(accentService.Events.SequenceEqual(["apply:First", "restore:First"])).IsTrue();
    }

    private static TestPipeline CreatePipeline(IOverrideElementGraphicsService graphicsService) => new(
        CreateIdentity(),
        graphicsService,
        [
            new OverrideElementsStep(["First"], new ViewGraphicsStyle()),
            new OverrideElementsStep(["Second"], new ViewGraphicsStyle())
        ]);

    private static ElementDiagnosticId CreateIdentity() => new(
        "TEST", "Test", "Test", DiagnosticSeverity.Message, true, false, string.Empty);

    private ElementVisualizationContext CreateContext() => new(
        _document!,
        GetView(_document!),
        new Dictionary<string, IReadOnlyCollection<ElementId>>
        {
            ["First"] = [CreateElementId(1)],
            ["Second"] = [CreateElementId(2)]
        });

    private static View GetView(Document document) => new FilteredElementCollector(document)
        .OfClass(typeof(View))
        .Cast<View>()
        .First(view => !view.IsTemplate);

    private static ElementId CreateElementId(int value)
#if BEFORE2024
        => new(value);
#else
        => new((long)value);
#endif

    private static Exception? CaptureException(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private sealed class TestPipeline(
        ElementDiagnosticId identity,
        IOverrideElementGraphicsService graphicsService,
        IReadOnlyList<ElementVisualizationStep> steps,
        IEnumerable<IAccentElementsService>? accentServices = null)
        : ElementVisualizationPipelineBase(
            identity, "Test", accentServices ?? [], graphicsService, new UnusedFilterService(),
            NullLogger<ElementVisualizationPipelineBase>.Instance)
    {
        protected override IReadOnlyList<ElementVisualizationStep> Steps { get; } = steps;
    }

    private sealed class RecordingAccentService : IAccentElementsService
    {
        public AccentElementsType Type => AccentElementsType.IsolateElementsOnView;
        public List<string> Events { get; } = [];

        public bool Execute(Document document, params ElementId[] elementIds) =>
            throw new InvalidOperationException("Execute should not be used by the pipeline.");

        public IElementAccentSession Apply(
            Document document,
            View view,
            IReadOnlyCollection<ElementId> elementIds)
        {
            string key = elementIds.Single() == CreateElementId(1) ? "First" : "Second";
            Events.Add($"apply:{key}");
            return new TestSession(() => Events.Add($"restore:{key}"));
        }
    }

    private sealed class RecordingGraphicsService(int throwOnApply = -1) : IOverrideElementGraphicsService
    {
        private int _applyCount;

        public List<string> Events { get; } = [];

        public IElementAccentSession Apply(
            Document document,
            View view,
            IReadOnlyCollection<ElementId> elementIds,
            ViewGraphicsStyle style)
        {
            _applyCount++;
            string key = elementIds.Single() == CreateElementId(1) ? "First" : "Second";
            Events.Add($"apply:{key}");
            if (_applyCount == throwOnApply)
                throw new InvalidOperationException("Apply failed.");

            return new TestSession(() => Events.Add($"restore:{key}"));
        }
    }

    private sealed class UnusedFilterService : IOverrideFilterGraphicsService
    {
        public IElementAccentSession Apply(
            Document document, View view, IReadOnlyCollection<ElementId> elementIds, ViewGraphicsStyle style) =>
            throw new InvalidOperationException("The filter service should not be used by this test.");
    }

    private sealed class TestSession(Action restore) : IElementAccentSession
    {
        private Action? _restore = restore;
        public void Restore() => Interlocked.Exchange(ref _restore, null)?.Invoke();
        public void Dispose() => Restore();
    }
}
