using Autodesk.Revit.DB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementAccentor.DI;
using TUnit.Core.Executors;

namespace Revit.Linter.ElementVisualization.RevitTests;

public sealed class ApplyElementGraphicsServiceTests : RevitApiTest
{
    private Document? _document;
    private Element? _element;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument()
    {
        _document = Application.NewProjectDocument(UnitSystem.Metric);
        using Transaction transaction = new(_document, "Create test element");
        transaction.Start();
        _element = Level.Create(_document, 0);
        transaction.Commit();
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Element_overrides_are_applied_and_restored()
    {
        using ServiceProvider services = CreateServices();
        IOverrideElementGraphicsService service = services.GetRequiredService<IOverrideElementGraphicsService>();
        View view = GetView();
        using OverrideGraphicSettings original = view.GetElementOverrides(_element!.Id);
        ViewGraphicsStyle style = new()
        {
            Halftone = true,
            Transparency = 45,
            ProjectionLines = new() { Color = new Color(12, 34, 56), Weight = 3 }
        };

        using IElementAccentSession session = service.Apply(_document!, view, [_element.Id], style);
        using OverrideGraphicSettings applied = view.GetElementOverrides(_element.Id);

        await Assert.That(applied.Halftone).IsTrue();
        await Assert.That(applied.Transparency).IsEqualTo(45);
        await Assert.That(applied.ProjectionLineColor.Red).IsEqualTo((byte)12);
        await Assert.That(applied.ProjectionLineWeight).IsEqualTo(3);

        session.Restore();
        using OverrideGraphicSettings restored = view.GetElementOverrides(_element.Id);
        await Assert.That(restored.Halftone).IsEqualTo(original.Halftone);
        await Assert.That(restored.Transparency).IsEqualTo(original.Transparency);
        await Assert.That(restored.ProjectionLineWeight).IsEqualTo(original.ProjectionLineWeight);
    }

    [Test]
    public async Task Selection_filter_is_deleted_on_restore()
    {
        using ServiceProvider services = CreateServices();
        IOverrideFilterGraphicsService service = services.GetRequiredService<IOverrideFilterGraphicsService>();
        View view = GetView();
        ElementId[] originalFilters = view.GetFilters().ToArray();

        using IElementAccentSession session = service.Apply(
            _document!,
            view,
            [_element!.Id],
            new ViewGraphicsStyle { Halftone = true });
        ElementId[] appliedFilters = view.GetFilters().Except(originalFilters).ToArray();

        await Assert.That(appliedFilters).Count().IsEqualTo(1);
        await Assert.That(_document!.GetElement(appliedFilters[0]) is SelectionFilterElement).IsTrue();

        session.Restore();
        await Assert.That(view.GetFilters()).IsEquivalentTo(originalFilters);
        await Assert.That(_document.GetElement(appliedFilters[0])).IsNull();
    }

    private static ServiceProvider CreateServices()
    {
        ServiceCollection services = new();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddElementAccentor();
        return services.BuildServiceProvider();
    }

    private View GetView() => new FilteredElementCollector(_document!)
        .OfClass(typeof(View))
        .Cast<View>()
        .First(view => !view.IsTemplate && view.AreGraphicsOverridesAllowed());
}
