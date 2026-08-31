using Toolkit.Revit.Extensions;

namespace Revit.Linter.CollisionDiagnostics.Infrastructure.Spatial;

/// <summary>
/// A coarse uniform grid over element bounding boxes, used to prune the candidate elements a
/// target must be checked against before the exact <c>BoundingBoxXYZExtensions.Overlaps</c> check
/// and the expensive Boolean solid intersection in <see cref="ElementDiagnostic"/>.
///
/// Without it, every target element in a group of size N is compared against all N elements in
/// that group (O(N^2) bounding-box comparisons total, even before any candidate actually
/// overlaps). Querying the grid narrows that down to the handful of cells the target's bounding
/// box spans, so total work across all N targets scales close to O(N) for a group whose elements
/// are roughly evenly distributed in space, instead of O(N^2).
///
/// This is a spatial hash (elements bucketed by integer cell coordinates in a dictionary), not a
/// balanced tree like an R-tree: no rebalancing cost to build or maintain, at the expense of
/// degrading toward a linear scan if a group is extremely unevenly distributed. Two specific
/// failure modes are guarded against explicitly rather than left as silent degradation:
///
/// - Cell size is the MEDIAN (not average/mean) of elements' largest bounding-box extent, so a
///   handful of outliers (a site element, a linked model, a badly authored family instance) can't
///   drag the cell size - and therefore everyone's grid resolution - toward "too coarse to
///   discriminate" or "too fine for the outliers to fit a few cells."
/// - Any single element/query whose bounding box would span more than <see cref="MaxCellsPerEntry"/>
///   cells (because it's huge relative to the median, or has non-finite/degenerate coordinates) is
///   NOT enumerated cell-by-cell. It's instead treated as "always a candidate" - present in every
///   query's result regardless of location - which is more expensive per occurrence but bounded and
///   safe: it never blows up memory/CPU building the index, and never silently drops an element
///   whose true position couldn't be indexed (which would otherwise mean a missed collision).
///
/// The index is built once per (document, rule group) and reused across every target element's
/// Execute call for that group - see ElementDiagnostic, which caches it the same way it already
/// caches the group's element list.
/// </summary>
internal sealed class BoundingBoxGridIndex
{
    // A box needing more than this many cells is treated as "always a candidate" instead of being
    // enumerated cell-by-cell - see the class remarks. 64 cells is generous enough that normally
    // sized elements (spanning a handful of cells per axis) never hit it, while still bounding the
    // worst case for one pathological element to a small, fixed amount of work.
    private const int MaxCellsPerEntry = 64;

    private readonly double _cellSize;
    private readonly Dictionary<(int X, int Y, int Z), List<Element>> _cells = [];
    private readonly List<Element> _uncellable = [];
    private readonly List<Element> _all = [];

    private BoundingBoxGridIndex(double cellSize)
    {
        _cellSize = cellSize;
    }

    public static BoundingBoxGridIndex Build(
        IEnumerable<Element> elements, Func<Element, BoundingBoxXYZ> getBoundingBox)
    {
        List<(Element Element, BoundingBoxXYZ Box)> entries = elements
            .Select(element => (Element: element, Box: getBoundingBox(element)))
            .ToList();

        var index = new BoundingBoxGridIndex(ComputeCellSize(entries));

        foreach ((Element element, BoundingBoxXYZ box) in entries)
            index.Insert(element, box);

        return index;
    }

    /// <summary>
    /// Returns every element whose bounding box shares at least one grid cell with <paramref name="box"/>,
    /// plus every element that couldn't be cell-indexed (see the class remarks). This is a coarse
    /// candidate set, not an exact overlap test - callers must still apply an exact bounding-box
    /// (and, ultimately, solid) intersection check to what's returned.
    /// </summary>
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
                    if (seen.Add(element.Id.Value()))
                        yield return element;
                }
            }

            foreach (Element element in _uncellable)
            {
                seen ??= [];
                if (seen.Add(element.Id.Value()))
                    yield return element;
            }
        }
        else
        {
            // The query box itself couldn't be resolved to a bounded cell range (huge or
            // degenerate/non-finite). We can't cheaply narrow this down to a handful of cells, so
            // fall back to every element rather than risk missing a real collision.
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

    // False when the box is non-finite/inverted, or would need more than MaxCellsPerEntry cells.
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

    // The MEDIAN (not mean) of elements' largest bounding-box extent. Using the median means a
    // small number of outliers (huge or tiny relative to the rest of the group) can't drag the
    // cell size away from what fits the typical element - those outliers are instead caught by
    // MaxCellsPerEntry in TryGetCellRange and handled via the uncellable fallback. Non-finite or
    // degenerate (near-zero) boxes are excluded from the calculation entirely so they can't skew it.
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
