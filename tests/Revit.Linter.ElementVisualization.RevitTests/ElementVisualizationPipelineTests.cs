using Autodesk.Revit.DB;
using Microsoft.Extensions.Logging;
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

    [Test]
    public async Task Restore_failure_throws_and_retains_sessions_for_retry()
    {
        // Locks the failure-containment contract the report presenter relies on when a restore
        // runs outside a valid API context: the failure is reported to the caller, the sessions
        // are retained for the next retry, and the streak is logged once as an error and then
        // as warnings rather than spamming the log on every trigger.
        CapturingLogger logger = new();
        using TestPipeline pipeline = new(
            CreateIdentity(),
            new RecordingGraphicsService(),
            [new AccentElementsStep(AccentElementsType.IsolateElementsOnView, ["First"])],
            [new RecordingAccentService(restoreThrows: true)],
            logger);

        bool applied = pipeline.Apply(CreateContext());
        Exception? firstRestore = CaptureException(pipeline.Restore);
        Exception? secondRestore = CaptureException(pipeline.Restore);

        await Assert.That(applied).IsTrue();
        await Assert.That(firstRestore).IsTypeOf<InvalidOperationException>();
        await Assert.That(secondRestore).IsTypeOf<InvalidOperationException>();
        await Assert.That(logger.Levels.Count(level => level == LogLevel.Error)).IsEqualTo(1);
        await Assert.That(logger.Levels.Count(level => level == LogLevel.Warning)).IsEqualTo(1);
        await Assert.That(logger.Levels
            .Where(level => level is LogLevel.Error or LogLevel.Warning)
            .SequenceEqual([LogLevel.Error, LogLevel.Warning])).IsTrue();
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
        IEnumerable<IAccentElementsService>? accentServices = null,
        ILogger? logger = null)
        : ElementVisualizationPipelineBase(
            identity, "Test", accentServices ?? [], graphicsService, new UnusedFilterService(),
            new UnusedCategoriesService(), new UnusedViewSettingsService(),
            logger ?? NullLogger<ElementVisualizationPipelineBase>.Instance)
    {
        protected override IReadOnlyList<ElementVisualizationStep> Steps { get; } = steps;
    }

    private sealed class RecordingAccentService(bool restoreThrows = false) : IAccentElementsService
    {
        public AccentElementsType Type => AccentElementsType.IsolateElementsOnView;
        public List<string> Events { get; } = [];

        public bool RunOnce(Document document, params ElementId[] elementIds) =>
            throw new InvalidOperationException("RunOnce should not be used by the pipeline.");

        public IRestoreSession Apply(
            Document document,
            View view,
            IReadOnlyCollection<ElementId> elementIds)
        {
            string key = elementIds.Single() == CreateElementId(1) ? "First" : "Second";
            Events.Add($"apply:{key}");
            return restoreThrows
                ? new ThrowingSession()
                : new TestSession(() => Events.Add($"restore:{key}"));
        }
    }

    private sealed class RecordingGraphicsService(int throwOnApply = -1) : IOverrideElementGraphicsService
    {
        private int _applyCount;

        public List<string> Events { get; } = [];

        public IRestoreSession Apply(
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
        public IRestoreSession Apply(
            Document document, View view, IReadOnlyCollection<ElementId> elementIds, ViewGraphicsStyle style) =>
            throw new InvalidOperationException("The filter service should not be used by this test.");
    }

    private sealed class UnusedCategoriesService : ISetCategoryVisibilityService
    {
        public IRestoreSession Apply(
            Document document, View view, IReadOnlyList<BuiltInCategory> categories) =>
            throw new InvalidOperationException("The categories service should not be used by this test.");
    }

    private sealed class UnusedViewSettingsService : IViewSettingsService
    {
        public IRestoreSession ApplyDetailLevel(Document document, View view, ViewDetailLevel level) =>
            throw new InvalidOperationException("The view settings service should not be used by this test.");

        public IRestoreSession ApplyDisplayStyle(Document document, View view, DisplayStyle style) =>
            throw new InvalidOperationException("The view settings service should not be used by this test.");
    }

    private sealed class TestSession(Action restore) : IRestoreSession
    {
        private Action? _restore = restore;
        public void Restore() => Interlocked.Exchange(ref _restore, null)?.Invoke();
        public void Dispose() => Restore();
    }

    private sealed class ThrowingSession : IRestoreSession
    {
        public void Restore() => throw new InvalidOperationException("Restore failed.");
        public void Dispose() { }
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<LogLevel> Levels { get; } = [];

        IDisposable ILogger.BeginScope<TState>(TState state) => new NullScope();

        bool ILogger.IsEnabled(LogLevel logLevel) => true;

        void ILogger.Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Levels.Add(logLevel);

        private sealed class NullScope : IDisposable
        {
            public void Dispose() { }
        }
    }
}
