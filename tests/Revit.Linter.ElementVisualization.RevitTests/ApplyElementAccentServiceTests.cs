using Autodesk.Revit.DB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementAccentor.DI;
using Revit.Linter.ElementVisualization.Abstractions.Models;
using Revit.Linter.ElementVisualization.Abstractions.Services;
using TUnit.Core.Executors;

namespace Revit.Linter.ElementVisualization.RevitTests;

public sealed class ApplyElementAccentServiceTests : RevitApiTest
{
    private Document? _document;
    private Wall? _wall;
    private View3D? _view;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument()
    {
        _document = Application.NewProjectDocument(UnitSystem.Metric);
        using Transaction transaction = new(_document, "Create visualization test model");
        transaction.Start();
        Level level = Level.Create(_document, 0);
        _wall = Wall.Create(
            _document,
            Line.CreateBound(XYZ.Zero, new XYZ(10, 0, 0)),
            level.Id,
            false);
        ViewFamilyType viewFamilyType = new FilteredElementCollector(_document)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .First(type => type.ViewFamily == ViewFamily.ThreeDimensional);
        _view = View3D.CreateIsometric(_document, viewFamilyType.Id);
        transaction.Commit();
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument()
    {
        _view?.Dispose();
        _document?.Close(false);
    }

    [Test]
    public async Task Isolation_is_applied_and_restored()
    {
        using ServiceProvider services = CreateServices();
        IAccentElementsService service = GetService(services, AccentElementsType.IsolateElementsOnView);

        using IElementAccentSession session = service.Apply(_document!, _view!, [_wall!.Id]);

        await Assert.That(_view!.IsTemporaryHideIsolateActive()).IsTrue();

        session.Restore();
        await Assert.That(_view.IsTemporaryHideIsolateActive()).IsFalse();

        session.Restore();
        await Assert.That(_view.IsTemporaryHideIsolateActive()).IsFalse();
    }

    [Test]
    public async Task Section_box_is_activated_and_restored()
    {
        using ServiceProvider services = CreateServices();
        IAccentElementsService service = GetService(services, AccentElementsType.CutViewByElements);
        using BoundingBoxXYZ original = _view!.GetSectionBox();
        bool originalActive = _view.IsSectionBoxActive;

        using IElementAccentSession session = service.Apply(_document!, _view, [_wall!.Id]);
        using BoundingBoxXYZ applied = _view.GetSectionBox();

        await Assert.That(_view.IsSectionBoxActive).IsTrue();
        await Assert.That(AreEqual(applied, original)).IsFalse();

        session.Restore();
        using BoundingBoxXYZ restored = _view.GetSectionBox();
        await Assert.That(_view.IsSectionBoxActive).IsEqualTo(originalActive);
        await Assert.That(AreEqual(restored, original)).IsTrue();

        session.Restore();
        await Assert.That(_view.IsSectionBoxActive).IsEqualTo(originalActive);
    }

    [Test]
    public async Task Combined_cut_and_isolation_pipeline_is_applied_and_restored()
    {
        using ServiceProvider services = CreateServices();
        bool originalSectionBoxActive = _view!.IsSectionBoxActive;
        using TestPipeline pipeline = new(
            services.GetServices<IAccentElementsService>(),
            services.GetRequiredService<IOverrideElementGraphicsService>(),
            services.GetRequiredService<IOverrideFilterGraphicsService>());
        ElementVisualizationContext context = new(
            _document!,
            _view,
            new Dictionary<string, IReadOnlyCollection<ElementId>>
            {
                ["Collision"] = [_wall!.Id]
            });

        bool result = pipeline.Apply(context);

        await Assert.That(result).IsTrue();
        await Assert.That(_view.IsSectionBoxActive).IsTrue();
        await Assert.That(_view.IsTemporaryHideIsolateActive()).IsTrue();

        pipeline.Restore();
        await Assert.That(_view.IsSectionBoxActive).IsEqualTo(originalSectionBoxActive);
        await Assert.That(_view.IsTemporaryHideIsolateActive()).IsFalse();
    }

    private static ServiceProvider CreateServices()
    {
        ServiceCollection services = new();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddElementAccentor();
        return services.BuildServiceProvider();
    }

    private static IAccentElementsService GetService(
        IServiceProvider services,
        AccentElementsType type) => services.GetServices<IAccentElementsService>()
        .Single(service => service.Type == type);

    private static bool AreEqual(BoundingBoxXYZ first, BoundingBoxXYZ second) =>
        first.Min.IsAlmostEqualTo(second.Min) &&
        first.Max.IsAlmostEqualTo(second.Max) &&
        first.Transform.AlmostEqual(second.Transform);

    private sealed class TestPipeline(
        IEnumerable<IAccentElementsService> accentServices,
        IOverrideElementGraphicsService elementGraphicsService,
        IOverrideFilterGraphicsService filterGraphicsService)
        : ElementVisualizationPipelineBase(
            new ElementDiagnosticId(
                "TEST", "Test", "Test", DiagnosticSeverity.Message, true, false, string.Empty),
            "Test",
            accentServices,
            elementGraphicsService,
            filterGraphicsService,
            NullLogger<ElementVisualizationPipelineBase>.Instance)
    {
        protected override IReadOnlyList<ElementVisualizationStep> Steps { get; } =
        [
            new AccentElementsStep(AccentElementsType.CutViewByElements, ["Collision"]),
            new AccentElementsStep(AccentElementsType.IsolateElementsOnView, ["Collision"])
        ];
    }
}
