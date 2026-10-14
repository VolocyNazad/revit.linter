using BenchmarkDotNet.Attributes;

namespace Revit.Linter.Benchmark.Benchmarks;

/// <summary>
/// Benchmarks the link-candidate partitioning step of the collision check with links enabled:
/// for each host target element, <c>ElementDiagnostic.FindLinkIntersections</c> runs
/// <c>GetLinkedGroups(document)[group].GroupBy(candidate => instanceId)</c> again, even though
/// the group content does not change between targets. That makes the partitioning cost
/// O(TargetCount * CandidateCount) per group instead of O(CandidateCount).
///
/// <c>Before</c> repeats the <c>GroupBy</c> per target (as production does now);
/// <c>After</c> partitions once into a dictionary and reuses it for every target
/// (the proposed fix). Both visit exactly <c>TargetCount * CandidateCount</c> candidates,
/// so equal return values prove the same work is done with less overhead.
///
/// Pure LINQ over lightweight records on purpose: no Revit API is involved, so this runs
/// headless without Revit and isolates the partitioning overhead from geometry costs.
/// </summary>
[MemoryDiagnoser]
public class LinkCandidatePartitionBenchmarks
{
    private const int TargetCount = 200;
    private const int CandidateCount = 800;
    private const int InstanceCount = 2;

    private List<LinkedCandidate> _groupCandidates = null!;
    private List<long> _targetIds = null!;

    /// <summary>
    /// Seeds the synthetic group: 800 linked candidates in 2 link instances and 200 host targets.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _groupCandidates = new List<LinkedCandidate>(CandidateCount);
        for (var i = 0; i < CandidateCount; i++)
            _groupCandidates.Add(new LinkedCandidate(ElementId: 100_000 + i, InstanceId: i % InstanceCount));

        _targetIds = new List<long>(TargetCount);
        for (var i = 0; i < TargetCount; i++)
            _targetIds.Add(200_000 + i);
    }

    /// <summary>
    /// Partitions the group with <c>GroupBy</c> separately for every target, as production does now.
    /// </summary>
    /// <returns>Visited candidate count; equal across variants proves the same work.</returns>
    [Benchmark(Baseline = true)]
    public long Before_GroupByPerTarget()
    {
        var visited = 0L;
        foreach (var targetId in _targetIds)
        {
            foreach (IGrouping<long, LinkedCandidate> instance in _groupCandidates.GroupBy(candidate => candidate.InstanceId))
            {
                foreach (LinkedCandidate candidate in instance)
                {
                    // Mirrors the per-candidate dictionary lookup production does after partitioning.
                    if (candidate.ElementId != targetId)
                        visited++;
                }
            }
        }

        return visited;
    }

    /// <summary>
    /// Partitions the group once and reuses the dictionary for every target (the proposed fix).
    /// </summary>
    /// <returns>Visited candidate count; equal across variants proves the same work.</returns>
    [Benchmark]
    public long After_CachedPartition()
    {
        Dictionary<long, List<LinkedCandidate>> byInstance = _groupCandidates
            .GroupBy(candidate => candidate.InstanceId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var visited = 0L;
        foreach (var targetId in _targetIds)
        {
            foreach (List<LinkedCandidate> instance in byInstance.Values)
            {
                foreach (LinkedCandidate candidate in instance)
                {
                    if (candidate.ElementId != targetId)
                        visited++;
                }
            }
        }

        return visited;
    }

    private sealed record LinkedCandidate(long ElementId, long InstanceId);
}
