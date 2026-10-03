using Revit.Sugar;

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
    private readonly Dictionary<(int X, int Y, int Z), List<Entry>> _cells = [];
    private readonly List<Entry> _uncellable = [];
    private readonly List<Entry> _all = [];

    private BoundingBoxGridIndex(double cellSize)
    {
        _cellSize = cellSize;
    }

    public static BoundingBoxGridIndex Build(
        IEnumerable<Element> elements, Func<Element, BoundingBoxXYZ> getBoundingBox)
    {
        List<Entry> entries = elements
            .Select(element => new Entry(element, element.Id.Value(), Bounds.From(getBoundingBox(element))))
            .ToList();

        var index = new BoundingBoxGridIndex(ComputeCellSize(entries));

        foreach (Entry entry in entries)
            index.Insert(entry);

        return index;
    }

    /// <summary>
    /// Returns every element whose bounding box may overlap <paramref name="box"/>: the elements sharing
    /// a grid cell with it plus the elements that couldn't be cell-indexed (see the class remarks), each
    /// kept only when its stored bounds touch or overlap the query bounds.
    /// </summary>
    /// <remarks>
    /// The bounds are copied into plain numbers when the index is built, so this pre-check costs no Revit
    /// API call and no cache lookup per candidate. That matters for the elements that are candidates of
    /// every query: with long elements such as pipes there can be hundreds of them, and checking each
    /// through the cached bounding-box service made that lookup the dominant cost of a collision rule.
    /// The pre-check never rejects a pair that could collide, so callers keep their exact bounding-box
    /// and solid checks for what is returned.
    /// </remarks>
    public IEnumerable<Element> Query(BoundingBoxXYZ box)
    {
        Bounds query = Bounds.From(box);

        if (!TryGetCellRange(query, out CellRange range))
        {
            // The query box itself couldn't be resolved to a bounded cell range (huge or
            // degenerate/non-finite). We can't cheaply narrow this down to a handful of cells, so
            // fall back to every element rather than risk missing a real collision.
            foreach (Entry entry in _all)
                if (entry.Bounds.MayOverlap(query))
                    yield return entry.Element;

            yield break;
        }

        HashSet<long>? seen = null;

        for (int x = range.MinX; x <= range.MaxX; x++)
            for (int y = range.MinY; y <= range.MaxY; y++)
                for (int z = range.MinZ; z <= range.MaxZ; z++)
                {
                    if (!_cells.TryGetValue((x, y, z), out List<Entry>? candidates)) continue;

                    foreach (Entry entry in candidates)
                    {
                        if (!entry.Bounds.MayOverlap(query)) continue;

                        seen ??= [];
                        if (seen.Add(entry.Id))
                            yield return entry.Element;
                    }
                }

        // An uncellable element is in no cell, so it cannot have been returned above.
        foreach (Entry entry in _uncellable)
            if (entry.Bounds.MayOverlap(query))
                yield return entry.Element;
    }

    private void Insert(Entry entry)
    {
        _all.Add(entry);

        if (!TryGetCellRange(entry.Bounds, out CellRange range))
        {
            _uncellable.Add(entry);
            return;
        }

        for (int x = range.MinX; x <= range.MaxX; x++)
            for (int y = range.MinY; y <= range.MaxY; y++)
                for (int z = range.MinZ; z <= range.MaxZ; z++)
                {
                    var cell = (x, y, z);
                    if (!_cells.TryGetValue(cell, out List<Entry>? bucket))
                        _cells[cell] = bucket = [];

                    bucket.Add(entry);
                }
    }

    // False when the box is non-finite/inverted, or would need more than MaxCellsPerEntry cells.
    private bool TryGetCellRange(Bounds box, out CellRange range)
    {
        range = default;

        if (!box.IsRegular) return false;

        int minX = ToCell(box.MinX), maxX = ToCell(box.MaxX);
        int minY = ToCell(box.MinY), maxY = ToCell(box.MaxY);
        int minZ = ToCell(box.MinZ), maxZ = ToCell(box.MaxZ);

        if (maxX < minX || maxY < minY || maxZ < minZ) return false;

        long cellCount = (long)(maxX - minX + 1) * (maxY - minY + 1) * (maxZ - minZ + 1);
        if (cellCount is <= 0 or > MaxCellsPerEntry) return false;

        range = new CellRange(minX, maxX, minY, maxY, minZ, maxZ);
        return true;
    }

    private int ToCell(double value) => (int)Math.Floor(value / _cellSize);

    // The MEDIAN (not mean) of elements' largest bounding-box extent. Using the median means a
    // small number of outliers (huge or tiny relative to the rest of the group) can't drag the
    // cell size away from what fits the typical element - those outliers are instead caught by
    // MaxCellsPerEntry in TryGetCellRange and handled via the uncellable fallback. Non-finite or
    // degenerate (near-zero) boxes are excluded from the calculation entirely so they can't skew it.
    private static double ComputeCellSize(List<Entry> entries)
    {
        List<double> extents = new(entries.Count);
        foreach (Entry entry in entries)
        {
            Bounds box = entry.Bounds;
            if (!box.IsRegular) continue;

            double extent = Math.Max(box.MaxX - box.MinX, Math.Max(box.MaxY - box.MinY, box.MaxZ - box.MinZ));
            if (extent > 1e-6) extents.Add(extent);
        }

        if (extents.Count == 0) return 1.0;

        extents.Sort();
        double median = extents[extents.Count / 2];
        return median > 1e-6 ? median : 1.0;
    }

    private readonly record struct CellRange(int MinX, int MaxX, int MinY, int MaxY, int MinZ, int MaxZ);

    private sealed record Entry(Element Element, long Id, Bounds Bounds);

    // A bounding box copied into plain numbers, so comparing two of them calls nothing in the Revit API.
    // IsRegular is stored rather than derived: a query compares its bounds with every element that does
    // not fit the grid cells, and recomputing the flag there doubled the cost of each comparison.
    private readonly record struct Bounds(
        double MinX, double MinY, double MinZ, double MaxX, double MaxY, double MaxZ, bool IsRegular)
    {
        public static Bounds From(BoundingBoxXYZ box)
        {
            XYZ min = box.Min;
            XYZ max = box.Max;
            double minX = min.X, minY = min.Y, minZ = min.Z;
            double maxX = max.X, maxY = max.Y, maxZ = max.Z;

            // Finite and not inverted, so the comparison in MayOverlap is meaningful.
            bool isRegular =
                IsFiniteNumber(minX) && IsFiniteNumber(minY) && IsFiniteNumber(minZ) &&
                IsFiniteNumber(maxX) && IsFiniteNumber(maxY) && IsFiniteNumber(maxZ) &&
                minX <= maxX && minY <= maxY && minZ <= maxZ;
            return new Bounds(minX, minY, minZ, maxX, maxY, maxZ, isRegular);
        }

        // Touching boxes count as overlapping and an irregular box overlaps everything: the check may
        // keep a pair that the exact test rejects later, but it never drops a pair that could collide.
        public bool MayOverlap(Bounds other) =>
            !IsRegular || !other.IsRegular ||
            (MinX <= other.MaxX && other.MinX <= MaxX &&
             MinY <= other.MaxY && other.MinY <= MaxY &&
             MinZ <= other.MaxZ && other.MinZ <= MaxZ);

        private static bool IsFiniteNumber(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}