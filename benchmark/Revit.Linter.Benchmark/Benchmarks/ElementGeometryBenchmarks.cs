using Autodesk.Revit.DB;
using BenchmarkDotNet.Attributes;
using Nice3point.BenchmarkDotNet.Revit;

namespace Revit.Linter.Benchmark.Benchmarks;

/// <summary>
/// Benchmarks solid collection on an element with solids (wall) before and after the ElementGeometryExtensions refactoring.
/// </summary>
public class ElementGeometryWithSolidsBenchmarks : RevitApiBenchmark
{
    private Document _document = null!;
    private Wall _wall = null!;
    private Level _level = null!;
    private Options _options = null!;

    protected sealed override void OnGlobalSetup()
    {
        _document = Application.NewProjectDocument(UnitSystem.Metric);

        using var transaction = new Transaction(_document, "Seed model");
        transaction.Start();

        _level = Level.Create(_document, 0);
        _wall = Wall.Create(_document, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)), _level.Id, false);

        transaction.Commit();

        _options = new Options { DetailLevel = ViewDetailLevel.Fine };
    }

    protected sealed override void OnGlobalCleanup()
    {
        _document.Close(false);
    }

    [Benchmark(Baseline = true)]
    public ICollection<Solid> Before_GetSolids_Element()
        => BeforeImplementation.GetSolids(_wall, _options);

    [Benchmark]
    public IReadOnlyCollection<Solid> After_GetSolids_Element()
        => AfterImplementation.GetSolids(_wall, _options);
}

/// <summary>
/// Benchmarks solid collection on an element without solids (level) before and after the ElementGeometryExtensions refactoring.
/// </summary>
public class ElementGeometryNoSolidsBenchmarks : RevitApiBenchmark
{
    private Document _document = null!;
    private Level _level = null!;
    private Options _options = null!;

    protected sealed override void OnGlobalSetup()
    {
        _document = Application.NewProjectDocument(UnitSystem.Metric);

        using var transaction = new Transaction(_document, "Seed model");
        transaction.Start();

        _level = Level.Create(_document, 0);

        transaction.Commit();

        _options = new Options { DetailLevel = ViewDetailLevel.Fine };
    }

    protected sealed override void OnGlobalCleanup()
    {
        _document.Close(false);
    }

    [Benchmark(Baseline = true)]
    public ICollection<Solid> Before_GetSolids_Element()
        => BeforeImplementation.GetSolids(_level, _options);

    [Benchmark]
    public IReadOnlyCollection<Solid> After_GetSolids_Element()
        => AfterImplementation.GetSolids(_level, _options);
}

internal static class BeforeImplementation
{
    private const double Epsilon = 1e-6;

    public static ICollection<Solid> GetSolids(Element element, Options options)
    {
        ICollection<Solid> collection = [];

        var geometryElement = element.get_Geometry(options);
        if (geometryElement is not null)
        {
            foreach (GeometryObject geometryObject in geometryElement)
                AddSolids(geometryObject, collection);
        }

        return collection;
    }

    private static void AddSolids(GeometryObject geometryObject, ICollection<Solid> collection)
    {
        if (geometryObject is Solid { Volume: > Epsilon } solid)
        {
            collection.Add(solid);
        }
        else if (geometryObject is GeometryInstance geometryInstance)
        {
            ExplodeGeometryInstance(geometryInstance, collection);
        }
    }

    private static void ExplodeGeometryInstance(GeometryInstance geometry, ICollection<Solid> collection)
    {
        foreach (GeometryObject geometryObject in geometry.GetInstanceGeometry())
            AddSolids(geometryObject, collection);
    }
}

internal static class AfterImplementation
{
    private const double Epsilon = 1e-6;

    public static IReadOnlyCollection<Solid> GetSolids(Element element, Options options)
    {
        List<Solid>? list = null;

        var geometryElement = element.get_Geometry(options);
        if (geometryElement is null)
        {
            return Array.Empty<Solid>();
        }

        foreach (GeometryObject geometryObject in geometryElement)
            CollectSolids(geometryObject, ref list);

        return list is not null ? list : Array.Empty<Solid>();
    }

    private static void CollectSolids(GeometryObject geometryObject, ref List<Solid>? list)
    {
        if (geometryObject is Solid { Volume: > Epsilon } solid)
        {
            (list ??= []).Add(solid);
        }
        else if (geometryObject is GeometryInstance geometryInstance)
        {
            foreach (GeometryObject nested in geometryInstance.GetInstanceGeometry())
                CollectSolids(nested, ref list);
        }
    }
}