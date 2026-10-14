using Autodesk.Revit.DB;
using BenchmarkDotNet.Attributes;
using Nice3point.BenchmarkDotNet.Revit;

namespace Revit.Linter.Benchmark.Benchmarks;

/// <summary>
/// Benchmarks a solid-level bounding-box prefilter for the collision narrow phase.
/// Production (<c>ElementDiagnostic.HasIntersection</c>) runs
/// <c>BooleanOperationsUtils.ExecuteBooleanOperation</c> on every solid pair whose element boxes
/// overlap. The proposal checks <c>Solid.GetBoundingBox</c> overlap first and runs the Boolean
/// only when the solid boxes touch: pairs the element-level check let through but whose solids
/// miss each other skip the expensive call entirely.
///
/// <c>Disjoint</c> pairs model that win (element boxes overlap, solids do not);
/// <c>Overlapping</c> pairs measure the prefilter overhead when the Boolean still runs.
/// <c>PairCount</c> is the number of candidate elements tested against one target, so the three
/// sizes show whether the ratio holds as the model grows. Equal return values across variants
/// prove identical verdicts.
/// </summary>
public class SolidBoundingBoxPrefilterBenchmarks : RevitApiBenchmark
{
    private const double Epsilon = 1e-6;

    // Revit API geometry is always expressed in feet internally. Candidates sit a fraction of the
    // wall thickness away from the target: every pair's element boxes overlap (so production would
    // run the Boolean), isolating the solid-level difference.
    private const double OffsetPerElementFeet = 0.01;

    /// <summary>
    /// The number of candidate elements tested against one target.
    /// </summary>
    [Params(10, 100, 400)]
    public int PairCount { get; set; }

    private Document _document = null!;
    private Solid _targetSolid = null!;
    private List<Solid> _overlappingSolids = null!;
    private List<Solid> _disjointSolids = null!;

    /// <summary>
    /// Seeds one target wall and the candidate walls plus their disjoint copies.
    /// </summary>
    protected sealed override void OnGlobalSetup()
    {
        _document = Application.NewProjectDocument(UnitSystem.Metric);

        using var transaction = new Transaction(_document, "Seed model");
        transaction.Start();

        FailureHandlingOptions failureOptions = transaction.GetFailureHandlingOptions()
            .SetFailuresPreprocessor(new DismissWarningsPreprocessor());
        transaction.SetFailureHandlingOptions(failureOptions);

        var level = Level.Create(_document, 0);
        var options = new Options { DetailLevel = ViewDetailLevel.Fine };

        var target = Wall.Create(
            _document, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)), level.Id, false);

        List<Wall> candidates = new(PairCount);
        for (var i = 0; i < PairCount; i++)
        {
            double offset = (i + 1) * OffsetPerElementFeet;
            candidates.Add(Wall.Create(
                _document, Line.CreateBound(new XYZ(0, offset, 0), new XYZ(10, offset, 0)), level.Id, false));
        }

        transaction.Commit();

        // Geometry is readable only after commit: the new walls are not regenerated before that.
        _targetSolid = FirstSolid(target.get_Geometry(options));

        _overlappingSolids = new List<Solid>(PairCount);
        _disjointSolids = new List<Solid>(PairCount);
        Transform farAway = Transform.CreateTranslation(new XYZ(500, 0, 0));
        foreach (Wall candidate in candidates)
        {
            Solid solid = FirstSolid(candidate.get_Geometry(options));
            _overlappingSolids.Add(solid);

            // Same real solid moved far away: solid boxes are disjoint while everything else about
            // the pair stays realistic. Element boxes would not overlap either in production, so
            // this isolates the narrow-phase cost of a pair the element check let through.
            _disjointSolids.Add(SolidUtils.CreateTransformed(solid, farAway));
        }
    }

    /// <summary>
    /// Closes the seeded document.
    /// </summary>
    protected sealed override void OnGlobalCleanup()
    {
        _document.Close(false);
    }

    /// <summary>
    /// Boolean on every pair, as production does now.
    /// </summary>
    /// <returns>Intersecting pair count; equal across variants proves identical verdicts.</returns>
    [Benchmark(Baseline = true)]
    public int Before_DisjointSolids()
    {
        var collisions = 0;
        foreach (Solid solid in _disjointSolids)
        {
            if (HasIntersection(solid, _targetSolid))
                collisions++;
        }

        return collisions;
    }

    /// <summary>
    /// Solid-box overlap first, Boolean only when the boxes touch (the proposed fix).
    /// </summary>
    /// <returns>Intersecting pair count; equal across variants proves identical verdicts.</returns>
    [Benchmark]
    public int After_DisjointSolids()
    {
        var collisions = 0;
        foreach (Solid solid in _disjointSolids)
        {
            if (SolidBoxesOverlap(solid, _targetSolid) && HasIntersection(solid, _targetSolid))
                collisions++;
        }

        return collisions;
    }

    /// <summary>
    /// Boolean on every overlapping pair, as production does now. No baseline here: only one
    /// baseline per class is allowed, so compare its mean with <c>After_OverlappingSolids</c> by hand.
    /// </summary>
    /// <returns>Intersecting pair count; equal across variants proves identical verdicts.</returns>
    [Benchmark]
    public int Before_OverlappingSolids()
    {
        var collisions = 0;
        foreach (Solid solid in _overlappingSolids)
        {
            if (HasIntersection(solid, _targetSolid))
                collisions++;
        }

        return collisions;
    }

    /// <summary>
    /// Prefilter overhead on pairs where the Boolean still runs.
    /// </summary>
    /// <returns>Intersecting pair count; equal across variants proves identical verdicts.</returns>
    [Benchmark]
    public int After_OverlappingSolids()
    {
        var collisions = 0;
        foreach (Solid solid in _overlappingSolids)
        {
            if (SolidBoxesOverlap(solid, _targetSolid) && HasIntersection(solid, _targetSolid))
                collisions++;
        }

        return collisions;
    }

    // Mirrors the proposed production prefilter. A solid box stores its bounds in local coordinates
    // plus a transform (unlike an element box), so the corners are moved into model coordinates
    // before comparing - touching boxes count as overlapping, never rejecting a real collision.
    private static bool SolidBoxesOverlap(Solid first, Solid second)
    {
        GetSolidBounds(first, out XYZ firstMin, out XYZ firstMax);
        GetSolidBounds(second, out XYZ secondMin, out XYZ secondMax);
        return firstMin.X <= secondMax.X && secondMin.X <= firstMax.X
            && firstMin.Y <= secondMax.Y && secondMin.Y <= firstMax.Y
            && firstMin.Z <= secondMax.Z && secondMin.Z <= firstMax.Z;
    }

    private static void GetSolidBounds(Solid solid, out XYZ min, out XYZ max)
    {
        BoundingBoxXYZ box = solid.GetBoundingBox();
        Transform transform = box.Transform;

        if (transform.IsIdentity)
        {
            min = box.Min;
            max = box.Max;
            return;
        }

        XYZ boxMin = box.Min;
        XYZ boxMax = box.Max;
        double minX = double.PositiveInfinity, minY = double.PositiveInfinity, minZ = double.PositiveInfinity;
        double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity, maxZ = double.NegativeInfinity;

        for (var x = 0; x < 2; x++)
        {
            for (var y = 0; y < 2; y++)
            {
                for (var z = 0; z < 2; z++)
                {
                    XYZ corner = transform.OfPoint(new XYZ(
                        x == 0 ? boxMin.X : boxMax.X,
                        y == 0 ? boxMin.Y : boxMax.Y,
                        z == 0 ? boxMin.Z : boxMax.Z));
                    minX = Math.Min(minX, corner.X);
                    minY = Math.Min(minY, corner.Y);
                    minZ = Math.Min(minZ, corner.Z);
                    maxX = Math.Max(maxX, corner.X);
                    maxY = Math.Max(maxY, corner.Y);
                    maxZ = Math.Max(maxZ, corner.Z);
                }
            }
        }

        min = new XYZ(minX, minY, minZ);
        max = new XYZ(maxX, maxY, maxZ);
    }

    private static Solid FirstSolid(GeometryElement? geometry)
    {
        if (geometry is not null)
        {
            foreach (GeometryObject geometryObject in geometry)
            {
                if (geometryObject is Solid { Volume: > Epsilon } solid)
                    return solid;
                if (geometryObject is GeometryInstance instance)
                {
                    foreach (GeometryObject nested in instance.GetInstanceGeometry())
                    {
                        if (nested is Solid { Volume: > Epsilon } nestedSolid)
                            return nestedSolid;
                    }
                }
            }
        }

        throw new InvalidOperationException("Seeded walls produced no solids; check the wall type geometry.");
    }

    private static bool HasIntersection(Solid first, Solid second)
    {
        try
        {
            return BooleanOperationsUtils.ExecuteBooleanOperation(
                first, second, BooleanOperationsType.Intersect).Volume > Epsilon;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException)
        {
            return true;
        }
    }

    // Same as in CollisionPairIntersectionBenchmarks: dismiss overlapping-walls warnings instead
    // of letting Revit apply its destructive default resolution in the headless host.
    private sealed class DismissWarningsPreprocessor : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            failuresAccessor.DeleteAllWarnings();
            return FailureProcessingResult.Continue;
        }
    }
}
