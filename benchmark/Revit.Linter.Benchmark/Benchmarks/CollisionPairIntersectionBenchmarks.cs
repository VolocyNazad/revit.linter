using Autodesk.Revit.DB;
using BenchmarkDotNet.Attributes;
using Nice3point.BenchmarkDotNet.Revit;

namespace Revit.Linter.Benchmark.Benchmarks;

/// <summary>
/// Benchmarks the collision-diagnostic scan (every element compared against every other element
/// in its group, as done by ElementDiagnostic.Execute driven by DiagnosticService's per-element loop)
/// before and after caching the pairwise Boolean solid-intersection result.
///
/// Because DiagnosticService.RunElementDiagnostics calls Execute once per element with that element
/// acting as the "target", every unordered pair (A, B) is evaluated twice: once as (target=A, candidate=B)
/// and once as (target=B, candidate=A). Before the fix, both evaluations recompute
/// BooleanOperationsUtils.ExecuteBooleanOperation from scratch. After the fix, the result is cached per
/// unordered pair and reused, so the expensive Boolean operation runs at most once per pair while both
/// elements still receive their own diagnostic feedback.
///
/// All walls in the fixture are seeded slightly offset from one another (a fraction of the wall
/// thickness) so every pair's solids still fully overlap - the worst case for the "Before"
/// implementation (every candidate that passes the bounding-box check also requires a real Boolean
/// intersection call) - without being literally coincident.
///
/// Overlapping/near-duplicate walls trigger a Revit warning ("Highlighted walls overlap...") whose
/// default resolution - applied automatically by Transaction.Commit() when there's no interactive
/// UI to answer the prompt, i.e. exactly this headless benchmark host - can delete one of the
/// overlapping walls. That silently invalidated Element references mid-run (InvalidObjectException
/// on get_Id()), independent of how large the offset was. The seed transaction below installs an
/// IFailuresPreprocessor that dismisses all warnings via FailuresAccessor.DeleteAllWarnings()
/// instead of letting Revit apply its own (destructive) default resolution, so every seeded wall is
/// guaranteed to survive the commit.
/// </summary>
public class CollisionPairIntersectionBenchmarks : RevitApiBenchmark
{
    // Revit API geometry is always expressed in feet internally, regardless of the document's
    // display unit system - 0.01 ft (~3 mm) per element keeps the cumulative offset well under a
    // default wall's thickness (~0.2 m) even at the largest ElementCount.
    private const double OffsetPerElementFeet = 0.01;

    private Document _document = null!;
    private List<Element> _elements = null!;
    private Options _options = null!;

    [Params(6, 12, 20)]
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
            double offset = i * OffsetPerElementFeet;
            var line = Line.CreateBound(new XYZ(0, offset, 0), new XYZ(10, offset, 0));
            _elements.Add(Wall.Create(_document, line, level.Id, false));
        }

        transaction.Commit();

        _options = new Options { DetailLevel = ViewDetailLevel.Fine };

        if (_elements.Any(element => !element.IsValidObject))
            throw new InvalidOperationException(
                "One or more seeded walls became invalid after commit; check for duplicate-element cleanup.");
    }

    // Dismisses every warning raised during the seed transaction (e.g. overlapping walls) without
    // applying Revit's own default resolution, which can otherwise delete one of the elements when
    // there's no interactive user to answer the prompt - exactly the situation in a headless
    // benchmark host.
    private sealed class DismissWarningsPreprocessor : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            failuresAccessor.DeleteAllWarnings();
            return FailureProcessingResult.Continue;
        }
    }

    protected sealed override void OnGlobalCleanup()
    {
        _document.Close(false);
    }

    [Benchmark(Baseline = true)]
    public int Before_ScanAllPairs()
        => CollisionScan.Before(_elements, _options);

    [Benchmark]
    public int After_ScanAllPairs()
        => CollisionScan.After(_elements, _options);
}

/// <summary>
/// Standalone reimplementation of ElementDiagnostic's collision scan, mirroring how
/// DiagnosticService drives it (every element is evaluated once as the "target" against
/// every other element in the group). Per-element geometry/bounding-box lookups are cached
/// by element id in both variants, matching GetElementGeometryService/GetElementBoundingBoxService
/// in production - only the pairwise Boolean-intersection caching differs between Before and After.
/// </summary>
internal static class CollisionScan
{
    private const double Epsilon = 1e-6;

    public static int Before(IReadOnlyList<Element> elements, Options options)
    {
        elements = ValidOnly(elements);
        var solidsById = new Dictionary<long, IReadOnlyCollection<Solid>>();
        var boxById = new Dictionary<long, BoundingBoxXYZ>();
        var collisions = 0;

        foreach (Element target in elements)
        {
            long targetId = target.Id.Value;
            IReadOnlyCollection<Solid> targetSolids = GetSolids(target, options, solidsById);
            if (targetSolids.Count == 0) continue;

            BoundingBoxXYZ targetBox = GetBox(target, options, boxById);

            foreach (Element candidate in elements)
            {
                long candidateId = candidate.Id.Value;
                if (candidateId == targetId) continue;

                BoundingBoxXYZ candidateBox = GetBox(candidate, options, boxById);
                if (!Overlaps(candidateBox, targetBox)) continue;

                IReadOnlyCollection<Solid> candidateSolids = GetSolids(candidate, options, solidsById);

                if (HasIntersection(candidateSolids, targetSolids))
                    collisions++;
            }
        }

        return collisions;
    }

    public static int After(IReadOnlyList<Element> elements, Options options)
    {
        elements = ValidOnly(elements);
        var solidsById = new Dictionary<long, IReadOnlyCollection<Solid>>();
        var boxById = new Dictionary<long, BoundingBoxXYZ>();
        var pairIntersects = new Dictionary<(long, long), bool>();
        var collisions = 0;

        foreach (Element target in elements)
        {
            long targetId = target.Id.Value;
            IReadOnlyCollection<Solid> targetSolids = GetSolids(target, options, solidsById);
            if (targetSolids.Count == 0) continue;

            BoundingBoxXYZ targetBox = GetBox(target, options, boxById);

            foreach (Element candidate in elements)
            {
                long candidateId = candidate.Id.Value;
                if (candidateId == targetId) continue;

                BoundingBoxXYZ candidateBox = GetBox(candidate, options, boxById);
                if (!Overlaps(candidateBox, targetBox)) continue;

                (long minId, long maxId) = targetId < candidateId ? (targetId, candidateId) : (candidateId, targetId);
                var pairKey = (minId, maxId);

                if (!pairIntersects.TryGetValue(pairKey, out bool intersects))
                {
                    IReadOnlyCollection<Solid> candidateSolids = GetSolids(candidate, options, solidsById);
                    intersects = HasIntersection(candidateSolids, targetSolids);
                    pairIntersects[pairKey] = intersects;
                }

                if (intersects) collisions++;
            }
        }

        return collisions;
    }

    private static IReadOnlyCollection<Solid> GetSolids(
        Element element, Options options, Dictionary<long, IReadOnlyCollection<Solid>> cache)
    {
        long id = element.Id.Value;
        if (cache.TryGetValue(id, out var cached)) return cached;

        List<Solid>? list = null;
        var geometryElement = element.get_Geometry(options);
        if (geometryElement is not null)
        {
            foreach (GeometryObject geometryObject in geometryElement)
                CollectSolids(geometryObject, ref list);
        }

        IReadOnlyCollection<Solid> solids = list is not null ? list : [];
        cache[id] = solids;
        return solids;
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

    private static BoundingBoxXYZ GetBox(Element element, Options options, Dictionary<long, BoundingBoxXYZ> cache)
    {
        long id = element.Id.Value;
        if (cache.TryGetValue(id, out var cached)) return cached;

        BoundingBoxXYZ box = element.get_Geometry(options).GetBoundingBox();
        cache[id] = box;
        return box;
    }

    // Defensive: skip any element that became invalid (e.g. deleted/undone) between setup and this
    // call, instead of letting Element.Id throw and crash the whole benchmark host. Applied equally
    // to Before and After, so it doesn't bias the comparison. Common case (nothing invalid) costs
    // only a single validity check per element and no allocation.
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

    private static bool Overlaps(BoundingBoxXYZ a, BoundingBoxXYZ b)
    {
        return a.Min.X <= b.Max.X && a.Max.X >= b.Min.X
            && a.Min.Y <= b.Max.Y && a.Max.Y >= b.Min.Y
            && a.Min.Z <= b.Max.Z && a.Max.Z >= b.Min.Z;
    }

    private static bool HasIntersection(IEnumerable<Solid> solids1, IEnumerable<Solid> solids2)
    {
        foreach (Solid solid in solids1)
        {
            foreach (Solid targetSolid in solids2)
            {
                try
                {
                    if (BooleanOperationsUtils.ExecuteBooleanOperation(
                        targetSolid, solid, BooleanOperationsType.Intersect).Volume > Epsilon) return true;
                }
                catch (Autodesk.Revit.Exceptions.InvalidOperationException)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
