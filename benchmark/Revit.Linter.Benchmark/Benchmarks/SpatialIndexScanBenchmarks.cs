using Autodesk.Revit.DB;
using BenchmarkDotNet.Attributes;
using Nice3point.BenchmarkDotNet.Revit;

namespace Revit.Linter.Benchmark.Benchmarks;

/// <summary>
/// Benchmarks the candidate-search step of the collision scan: for each element acting as the
/// "target" (as DiagnosticService drives ElementDiagnostic.Execute), find which other elements in
/// its group could plausibly collide, using a bounding-box comparison. This isolates the O(N) vs
/// O(N^2) difference of BoundingBoxGridIndex from the Boolean-solid-intersection cost already
/// covered by CollisionPairIntersectionBenchmarks - no Boolean operations happen here at all.
///
/// Unlike CollisionPairIntersectionBenchmarks (where every wall shares the same location so every
/// pair overlaps - the worst case for redundant Boolean-op recomputation), the walls here are
/// spread far apart along a line. That's the scenario a spatial index is actually for: in a large,
/// spread-out model, most element pairs are nowhere near each other, so a linear scan wastes almost
/// all of its bounding-box comparisons on pairs that were never going to overlap, while a grid
/// query only ever looks at the handful of cells near the target.
/// </summary>
public class SpatialIndexScanBenchmarks : RevitApiBenchmark
{
    // Revit API geometry is always expressed in feet internally. 50 ft between wall origins is far
    // larger than any single wall's bounding box, so (with the default wall type) only truly
    // neighboring walls' bounding boxes ever come close to overlapping - most pairs in the group
    // are unrelated, which is exactly the case a spatial index prunes away.
    private const double ElementSpacingFeet = 50.0;

    private Document _document = null!;
    private List<Element> _elements = null!;
    private Options _options = null!;

    [Params(50, 200, 800)]
    public int ElementCount { get; set; }

    protected sealed override void OnGlobalSetup()
    {
        _document = Application.NewProjectDocument(UnitSystem.Metric);

        using var transaction = new Transaction(_document, "Seed model");
        transaction.Start();

        FailureHandlingOptions failureOptions = transaction.GetFailureHandlingOptions()
            .SetFailuresPreprocessor(new DismissWarningsPreprocessor());
        transaction.SetFailureHandlingOptions(failureOptions);

        var level = Level.Create(_document, 0);
        _elements = [];

        for (var i = 0; i < ElementCount; i++)
        {
            double x = i * ElementSpacingFeet;
            var line = Line.CreateBound(new XYZ(x, 0, 0), new XYZ(x + 10, 0, 0));
            _elements.Add(Wall.Create(_document, line, level.Id, false));
        }

        transaction.Commit();

        _options = new Options { DetailLevel = ViewDetailLevel.Fine };

        if (_elements.Any(element => !element.IsValidObject))
            throw new InvalidOperationException(
                "One or more seeded walls became invalid after commit; check for duplicate-element cleanup.");
    }

    protected sealed override void OnGlobalCleanup()
    {
        _document.Close(false);
    }

    [Benchmark(Baseline = true)]
    public int Before_LinearScan()
        => SpatialScan.Before(_elements, _options);

    [Benchmark]
    public int After_GridIndex()
        => SpatialScan.After(_elements, _options);

    // See CollisionPairIntersectionBenchmarks for why this is needed: Transaction.Commit() applies
    // Revit's default warning resolution when there's no interactive UI, which can delete one of a
    // pair of overlapping/duplicate elements. Dismissing warnings instead of letting Revit resolve
    // them keeps every seeded wall intact.
    private sealed class DismissWarningsPreprocessor : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            failuresAccessor.DeleteAllWarnings();
            return FailureProcessingResult.Continue;
        }
    }
}

/// <summary>
/// Standalone reimplementation of ElementDiagnostic's candidate search, mirroring how
/// DiagnosticService drives it (every element is evaluated once as the "target" against its
/// group). Before does the linear scan ElementDiagnostic used to do; After builds a
/// BoundingBoxGridIndex-equivalent grid once (as ElementDiagnostic now caches it per document/rule
/// group) and queries it per target. The production BoundingBoxGridIndex is internal to a
/// different assembly, so its grid logic is reimplemented here as SpatialGrid - kept in lockstep
/// with src/Revit.Linter.CollisionDiagnostics/Infrastructure/Spatial/BoundingBoxGridIndex.cs.
/// </summary>
internal static class SpatialScan
{
    public static int Before(IReadOnlyList<Element> elements, Options options)
    {
        elements = ValidOnly(elements);
        var boxById = new Dictionary<long, BoundingBoxXYZ>();
        var overlappingPairs = 0;

        foreach (Element target in elements)
        {
            long targetId = target.Id.Value;
            BoundingBoxXYZ targetBox = GetBox(target, options, boxById);

            foreach (Element candidate in elements)
            {
                long candidateId = candidate.Id.Value;
                if (candidateId == targetId) continue;

                BoundingBoxXYZ candidateBox = GetBox(candidate, options, boxById);
                if (Overlaps(candidateBox, targetBox))
                    overlappingPairs++;
            }
        }

        return overlappingPairs;
    }

    public static int After(IReadOnlyList<Element> elements, Options options)
    {
        elements = ValidOnly(elements);
        var boxById = new Dictionary<long, BoundingBoxXYZ>();

        SpatialGrid index = SpatialGrid.Build(elements, element => GetBox(element, options, boxById));

        var overlappingPairs = 0;

        foreach (Element target in elements)
        {
            long targetId = target.Id.Value;
            BoundingBoxXYZ targetBox = GetBox(target, options, boxById);

            foreach (Element candidate in index.Query(targetBox))
            {
                long candidateId = candidate.Id.Value;
                if (candidateId == targetId) continue;

                BoundingBoxXYZ candidateBox = GetBox(candidate, options, boxById);
                if (Overlaps(candidateBox, targetBox))
                    overlappingPairs++;
            }
        }

        return overlappingPairs;
    }

    private static BoundingBoxXYZ GetBox(Element element, Options options, Dictionary<long, BoundingBoxXYZ> cache)
    {
        long id = element.Id.Value;
        if (cache.TryGetValue(id, out var cached)) return cached;

        BoundingBoxXYZ box = element.get_Geometry(options).GetBoundingBox();
        cache[id] = box;
        return box;
    }

    private static bool Overlaps(BoundingBoxXYZ a, BoundingBoxXYZ b)
    {
        return a.Min.X <= b.Max.X && a.Max.X >= b.Min.X
            && a.Min.Y <= b.Max.Y && a.Max.Y >= b.Min.Y
            && a.Min.Z <= b.Max.Z && a.Max.Z >= b.Min.Z;
    }

    // Defensive: skip any element that became invalid between setup and this call, instead of
    // letting Element.Id throw and crash the whole benchmark host. Applied equally to Before and
    // After, so it doesn't bias the comparison.
    private static IReadOnlyList<Element> ValidOnly(IReadOnlyList<Element> elements)
    {
        var anyInvalid = false;
        for (var i = 0; i < elements.Count; i++)
        {
            if (elements[i].IsValidObject) continue;
            anyInvalid = true;
            break;
        }

        if (!anyInvalid) return elements;

        var filtered = new List<Element>(elements.Count);
        foreach (Element element in elements)
        {
            if (element.IsValidObject)
                filtered.Add(element);
        }

        return filtered;
    }
}

/// <summary>
/// Mirrors src/Revit.Linter.CollisionDiagnostics/Infrastructure/Spatial/BoundingBoxGridIndex.cs -
/// see that file for the full rationale. Kept as a separate copy here because the production type
/// is internal to a different assembly the benchmark project doesn't (and shouldn't) reference.
/// </summary>
internal sealed class SpatialGrid
{
    private const int MaxCellsPerEntry = 64;

    private readonly double _cellSize;
    private readonly Dictionary<(int X, int Y, int Z), List<Element>> _cells = [];
    private readonly List<Element> _uncellable = [];
    private readonly List<Element> _all = [];

    private SpatialGrid(double cellSize)
    {
        _cellSize = cellSize;
    }

    public static SpatialGrid Build(IEnumerable<Element> elements, Func<Element, BoundingBoxXYZ> getBoundingBox)
    {
        List<(Element Element, BoundingBoxXYZ Box)> entries = elements
            .Select(element => (Element: element, Box: getBoundingBox(element)))
            .ToList();

        var index = new SpatialGrid(ComputeCellSize(entries));

        foreach ((Element element, BoundingBoxXYZ box) in entries)
            index.Insert(element, box);

        return index;
    }

    public IEnumerable<Element> Query(BoundingBoxXYZ box)
    {
        HashSet<long>? seen = null;

        if (TryGetCellRange(box, out CellRange range))
        {
            for (int x = range.MinX; x <= range.MaxX; x++)
            for (int y = range.MinY; y <= range.MaxY; y++)
            for (int z = range.MinZ; z <= range.MaxZ; z++)
            {
                if (!_cells.TryGetValue((x, y, z), out List<Element>? candidates)) continue;

                foreach (Element element in candidates)
                {
                    seen ??= [];
                    if (seen.Add(element.Id.Value))
                        yield return element;
                }
            }

            foreach (Element element in _uncellable)
            {
                seen ??= [];
                if (seen.Add(element.Id.Value))
                    yield return element;
            }
        }
        else
        {
            foreach (Element element in _all)
                yield return element;
        }
    }

    private void Insert(Element element, BoundingBoxXYZ box)
    {
        _all.Add(element);

        if (!TryGetCellRange(box, out CellRange range))
        {
            _uncellable.Add(element);
            return;
        }

        for (int x = range.MinX; x <= range.MaxX; x++)
        for (int y = range.MinY; y <= range.MaxY; y++)
        for (int z = range.MinZ; z <= range.MaxZ; z++)
        {
            var cell = (x, y, z);
            if (!_cells.TryGetValue(cell, out List<Element>? bucket))
                _cells[cell] = bucket = [];

            bucket.Add(element);
        }
    }

    private bool TryGetCellRange(BoundingBoxXYZ box, out CellRange range)
    {
        range = default;

        if (!IsFinite(box.Min) || !IsFinite(box.Max)) return false;

        int minX = ToCell(box.Min.X), maxX = ToCell(box.Max.X);
        int minY = ToCell(box.Min.Y), maxY = ToCell(box.Max.Y);
        int minZ = ToCell(box.Min.Z), maxZ = ToCell(box.Max.Z);

        if (maxX < minX || maxY < minY || maxZ < minZ) return false;

        long cellCount = (long)(maxX - minX + 1) * (maxY - minY + 1) * (maxZ - minZ + 1);
        if (cellCount is <= 0 or > MaxCellsPerEntry) return false;

        range = new CellRange(minX, maxX, minY, maxY, minZ, maxZ);
        return true;
    }

    private int ToCell(double value) => (int)Math.Floor(value / _cellSize);

    private static bool IsFinite(XYZ point) =>
        double.IsFinite(point.X) && double.IsFinite(point.Y) && double.IsFinite(point.Z);

    private static double ComputeCellSize(List<(Element Element, BoundingBoxXYZ Box)> entries)
    {
        List<double> extents = new(entries.Count);
        foreach ((_, BoundingBoxXYZ box) in entries)
        {
            if (!IsFinite(box.Min) || !IsFinite(box.Max)) continue;

            XYZ size = box.Max - box.Min;
            double extent = Math.Max(size.X, Math.Max(size.Y, size.Z));
            if (extent > 1e-6) extents.Add(extent);
        }

        if (extents.Count == 0) return 1.0;

        extents.Sort();
        double median = extents[extents.Count / 2];
        return median > 1e-6 ? median : 1.0;
    }

    private readonly record struct CellRange(int MinX, int MaxX, int MinY, int MaxY, int MinZ, int MaxZ);
}
