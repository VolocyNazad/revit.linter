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

/// <summary>
/// Covers persistent category visibility through <c>ISetCategoryVisibilityService</c>: only the
/// listed categories stay visible, restoring unhides exactly the hidden ones.
/// </summary>
/// <remarks>
/// Runs inside live Revit like the other accentor tests; verify locally with the bridge.
/// </remarks>
public sealed class SetCategoryVisibilityServiceTests : RevitApiTest
{
    private Document? _document;
    private Wall? _wall;
    private View3D? _view;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument()
    {
        _document = Application.NewProjectDocument(UnitSystem.Metric);
        using Transaction transaction = new(_document, "Create category visibility test model");
        transaction.Start();
        Level level = Level.Create(_document, 0);
        _wall = Wall.Create(
            _document,
            Line.CreateBound(XYZ.Zero, new XYZ(10, 0, 0)),
            level.Id,
            false);
        _ = CreateBox(_document, 20, 0, 2, 2);
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
    public async Task Only_listed_categories_stay_visible()
    {
        using ServiceProvider services = CreateServices();
        ISetCategoryVisibilityService service = services.GetRequiredService<ISetCategoryVisibilityService>();

        using IRestoreSession session = service.Apply(
            _document!, _view!, [BuiltInCategory.OST_Walls]);

        // Persistent visibility composes with the temporary mode instead of using it.
        await Assert.That(_view!.IsTemporaryHideIsolateActive()).IsFalse();
        await Assert.That(GetCategoryHidden(BuiltInCategory.OST_Walls)).IsFalse();
        await Assert.That(GetCategoryHidden(BuiltInCategory.OST_GenericModel)).IsTrue();

        session.Restore();
        await Assert.That(GetCategoryHidden(BuiltInCategory.OST_Walls)).IsFalse();
        await Assert.That(GetCategoryHidden(BuiltInCategory.OST_GenericModel)).IsFalse();

        session.Restore();
        await Assert.That(GetCategoryHidden(BuiltInCategory.OST_GenericModel)).IsFalse();
    }

    [Test]
    public async Task Leaves_previously_hidden_categories_hidden_after_restore()
    {
        Document document = _document!;
        View3D view = _view!;
        using (Transaction transaction = new(document, "Hide generic models before the test"))
        {
            transaction.Start();
            view.SetCategoryHidden(GetCategoryId(BuiltInCategory.OST_GenericModel), true);
            transaction.Commit();
        }

        using ServiceProvider services = CreateServices();
        ISetCategoryVisibilityService service = services.GetRequiredService<ISetCategoryVisibilityService>();
        using IRestoreSession session = service.Apply(
            document, view, [BuiltInCategory.OST_Walls]);

        // The generic models were already hidden, so there is nothing to change for them.
        await Assert.That(GetCategoryHidden(BuiltInCategory.OST_Walls)).IsFalse();
        await Assert.That(GetCategoryHidden(BuiltInCategory.OST_GenericModel)).IsTrue();

        session.Restore();
        await Assert.That(GetCategoryHidden(BuiltInCategory.OST_GenericModel)).IsTrue();
    }

    [Test]
    public async Task Works_while_temporary_isolate_is_active()
    {
        using ServiceProvider services = CreateServices();
        IAccentElementsService elementsService = services.GetServices<IAccentElementsService>()
            .Single(service => service.Type == AccentElementsType.IsolateElementsOnView);
        ISetCategoryVisibilityService categoriesService =
            services.GetRequiredService<ISetCategoryVisibilityService>();
        using IRestoreSession isolation = elementsService.Apply(_document!, _view!, [_wall!.Id]);

        using IRestoreSession visibility = categoriesService.Apply(
            _document!, _view!, [BuiltInCategory.OST_Walls]);

        await Assert.That(_view!.IsTemporaryHideIsolateActive()).IsTrue();
        await Assert.That(GetCategoryHidden(BuiltInCategory.OST_GenericModel)).IsTrue();

        visibility.Restore();
        await Assert.That(GetCategoryHidden(BuiltInCategory.OST_GenericModel)).IsFalse();
        await Assert.That(_view.IsTemporaryHideIsolateActive()).IsTrue();
    }

    private ElementId GetCategoryId(BuiltInCategory category) =>
        Category.GetCategory(_document!, category)!.Id;

    private bool GetCategoryHidden(BuiltInCategory category) =>
        _view!.GetCategoryHidden(GetCategoryId(category));

    private static ServiceProvider CreateServices()
    {
        ServiceCollection services = new();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddElementAccentor();
        return services.BuildServiceProvider();
    }

    // A direct shape keeps exactly the solid it is given, unlike walls, which Revit joins and trims.
    private static DirectShape CreateBox(Document document, double minX, double minY, double sizeX, double sizeY)
    {
        XYZ corner0 = new(minX, minY, 0);
        XYZ corner1 = new(minX + sizeX, minY, 0);
        XYZ corner2 = new(minX + sizeX, minY + sizeY, 0);
        XYZ corner3 = new(minX, minY + sizeY, 0);
        List<Curve> edges =
        [
            Line.CreateBound(corner0, corner1),
            Line.CreateBound(corner1, corner2),
            Line.CreateBound(corner2, corner3),
            Line.CreateBound(corner3, corner0),
        ];
        Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(
            new List<CurveLoop> { CurveLoop.Create(edges) }, XYZ.BasisZ, 2);

        DirectShape shape = DirectShape.CreateElement(
            document, new ElementId(BuiltInCategory.OST_GenericModel));
        shape.SetShape(new List<GeometryObject> { solid });
        return shape;
    }
}
