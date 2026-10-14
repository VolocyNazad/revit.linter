using Autodesk.Revit.DB;
using BenchmarkDotNet.Attributes;
using Nice3point.BenchmarkDotNet.Revit;

namespace Revit.Linter.Benchmark.Benchmarks;

/// <summary>
/// Benchmarks the link-solids transform strategy of the collision check with links enabled.
/// The superseded production path moved every overlapping linked solid into host coordinates
/// via <c>SolidUtils.CreateTransformed</c>: one transform call per candidate solid, i.e.
/// O(overlapping candidates) transforms per target element.
///
/// <c>Before</c> does exactly that; <c>After</c> applies the inverse transform to the target
/// solids once per link instance and intersects them with the native (link-local) candidate
/// solids: O(link instances) transforms per target instead. The Boolean intersection count is
/// identical in both variants; only the number of <c>CreateTransformed</c> calls differs
/// (12 candidates vs 2 instances here, so ~6x fewer transforms).
///
/// Both transforms are rigid (translation; rotation about the target center), so volumes are
/// preserved and equal return values prove identical verdicts.
/// </summary>
public class LinkSolidsTransformBenchmarks : RevitApiBenchmark
{
    private const double Epsilon = 1e-6;

    // Revit API geometry is always expressed in feet internally. Candidates are offset by a
    // fraction of the wall thickness so every pair still overlaps (real Boolean work, no early
    // exit), mirroring CollisionPairIntersectionBenchmarks.
    private const double OffsetPerElementFeet = 0.01;
    private const int CandidateCount = 12;
    private const int InstanceCount = 2;

    private Document _document = null!;
    private List<Solid> _targetSolids = null!;
    private List<List<Solid>> _candidateWallSolids = null!;
    private List<Transform> _instanceTransforms = null!;

    /// <summary>
    /// Seeds one target wall and twelve overlapping candidate walls plus two link transforms.
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

        var target = Wall.Create(
            _document, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)), level.Id, false);

        List<Wall> candidates = new(CandidateCount);
        for (var i = 0; i < CandidateCount; i++)
        {
            double offset = (i + 1) * OffsetPerElementFeet;
            candidates.Add(Wall.Create(
                _document, Line.CreateBound(new XYZ(0, offset, 0), new XYZ(10, offset, 0)), level.Id, false));
        }

        transaction.Commit();

        // Geometry is readable only after commit: the new walls are not regenerated before that.
        var options = new Options { DetailLevel = ViewDetailLevel.Fine };
        _targetSolids = CollectSolids(target.get_Geometry(options));

        _candidateWallSolids = new List<List<Solid>>(CandidateCount);
        foreach (Wall candidate in candidates)
            _candidateWallSolids.Add(CollectSolids(candidate.get_Geometry(options)));

        // Small enough that transformed candidates still overlap the target: real Boolean work
        // on every pair in both variants.
        _instanceTransforms =
        [
            Transform.CreateTranslation(new XYZ(0.05, 0, 0)),
            Transform.CreateRotationAtPoint(XYZ.BasisZ, 5.0 * Math.PI / 180.0, new XYZ(5, 0, 0)),
        ];

        if (_targetSolids.Count == 0 || _candidateWallSolids.Any(wall => wall.Count == 0))
            throw new InvalidOperationException("Seeded walls produced no solids; check the wall type geometry.");
    }

    /// <summary>
    /// Closes the seeded document.
    /// </summary>
    protected sealed override void OnGlobalCleanup()
    {
        _document.Close(false);
    }

    /// <summary>
    /// Moves every candidate solid into host coordinates, as production does now.
    /// </summary>
    /// <returns>Overlapping candidate count; equal across variants proves identical verdicts.</returns>
    [Benchmark(Baseline = true)]
    public int Before_TransformCandidatePerPair()
    {
        var collisions = 0;
        var perInstance = CandidateCount / InstanceCount;

        for (var i = 0; i < CandidateCount; i++)
        {
            Transform transform = _instanceTransforms[i / perInstance];
            List<Solid> transformed =
                _candidateWallSolids[i].Select(solid => SolidUtils.CreateTransformed(solid, transform)).ToList();
            if (HasIntersection(transformed, _targetSolids))
                collisions++;
        }

        return collisions;
    }

    /// <summary>
    /// Moves the target solids once per link instance and intersects native candidate solids (the proposed fix).
    /// </summary>
    /// <returns>Overlapping candidate count; equal across variants proves identical verdicts.</returns>
    [Benchmark]
    public int After_TransformTargetOncePerInstance()
    {
        var collisions = 0;
        var perInstance = CandidateCount / InstanceCount;

        for (var instance = 0; instance < InstanceCount; instance++)
        {
            Transform inverse = _instanceTransforms[instance].Inverse;
            List<Solid> targetInLink =
                _targetSolids.Select(solid => SolidUtils.CreateTransformed(solid, inverse)).ToList();

            for (var i = 0; i < perInstance; i++)
            {
                if (HasIntersection(_candidateWallSolids[instance * perInstance + i], targetInLink))
                    collisions++;
            }
        }

        return collisions;
    }

    private static List<Solid> CollectSolids(GeometryElement? geometry)
    {
        List<Solid> solids = [];
        if (geometry is null)
            return solids;

        foreach (GeometryObject geometryObject in geometry)
            CollectSolids(geometryObject, solids);

        return solids;
    }

    private static void CollectSolids(GeometryObject geometryObject, List<Solid> solids)
    {
        if (geometryObject is Solid { Volume: > Epsilon } solid)
        {
            solids.Add(solid);
        }
        else if (geometryObject is GeometryInstance geometryInstance)
        {
            foreach (GeometryObject nested in geometryInstance.GetInstanceGeometry())
                CollectSolids(nested, solids);
        }
    }

    private static bool HasIntersection(IEnumerable<Solid> first, IEnumerable<Solid> second)
    {
        foreach (Solid solid in first)
        {
            foreach (Solid other in second)
            {
                try
                {
                    if (BooleanOperationsUtils.ExecuteBooleanOperation(
                        other, solid, BooleanOperationsType.Intersect).Volume > Epsilon)
                        return true;
                }
                catch (Autodesk.Revit.Exceptions.InvalidOperationException)
                {
                    return true;
                }
            }
        }

        return false;
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
