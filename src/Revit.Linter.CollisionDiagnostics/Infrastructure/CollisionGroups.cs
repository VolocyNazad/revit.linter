using Microsoft.Extensions.Logging;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Extensions;
using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.CollisionDiagnostics.Infrastructure;

/// <summary>
/// Partitions the elements selected by a collision rule by its <c>GroupBy</c> formula.
/// </summary>
/// <remarks>
/// The partition is cached once per (document, view, formulas): every target element of the run reads
/// the same groups. Elements without geometry cannot intersect anything and their bounding box cannot
/// be built, so they stay out of every group instead of failing the spatial index later.
/// </remarks>
internal sealed class CollisionGroups(
    CollisionElementPool elementPool,
    IDocumentQueryService documentQueries,
    ILogger logger,
    ElementDiagnosticId identity)
{
    /// <summary>Gets the element groups of the rule, creating them on a cache miss.</summary>
    /// <param name="document">The queried document.</param>
    /// <param name="view">The view the selection is limited to, or <see langword="null"/> for the whole document.</param>
    /// <returns>The cached groups.</returns>
    internal ElementGroups Get(Document document, View? view) =>
        documentQueries.GetCached(
            document, view, CollisionQuery.Groups,
            () => Build(elementPool.Collect(document, view), view),
            elementPool.TakeFormula, elementPool.GroupByFormula);

    /// <summary>Gets the group of one target element.</summary>
    /// <param name="groups">The groups of the current run.</param>
    /// <param name="element">The target element.</param>
    /// <returns>The target group name.</returns>
    /// <remarks>
    /// An element that was already collected resolves through the partition. Any other target — for
    /// example one filtered out of the collected pool because it has no geometry — is grouped by
    /// evaluating the grouping formula once.
    /// </remarks>
    internal string GroupOf(ElementGroups groups, Element element) =>
        groups.GroupByElementId.TryGetValue(element.Id.IdValue(), out string? knownGroup)
            ? knownGroup
            : elementPool.GroupOf(element);

    private ElementGroups Build(IList<Element> elements, View? view)
    {
        // GetGeometry is cached, and the bounding-box service derives from the same cached geometry.
        List<Element> testable = elements
            .Where(element => documentQueries.GetGeometry(element, view) is not null)
            .ToList();
        if (testable.Count < elements.Count)
        {
            // Logged once per rule run: groups are built once per run through the query cache.
            // Such elements cannot collide, but selecting them still costs a geometry read each,
            // so narrowing the take filters to the reviewed categories speeds the check up.
            logger.LogWarning(
                "Collision diagnostic {DiagnosticCode} skipped {SkippedElementCount} selected elements " +
                "without geometry; they cannot collide. Narrow the take filters to the reviewed categories " +
                "to avoid selecting them.",
                identity.Code, elements.Count - testable.Count);
        }

        Dictionary<long, string> groupByElementId = new(testable.Count);
        ILookup<string, Element> byGroup = testable.ToLookup(element =>
        {
            string group = elementPool.GroupOf(element);
            groupByElementId[element.Id.IdValue()] = group;
            return group;
        });
        return new ElementGroups(byGroup, groupByElementId);
    }
}

/// <summary>
/// The collected elements partitioned by the <c>GroupBy</c> formula, indexed both ways:
/// <c>ByGroup</c> answers "which elements are in this group" (keyed by group name),
/// <c>GroupByElementId</c> answers "which group does this element belong to" (keyed by element
/// identifier). Both are built in one pass so they cannot diverge.
/// </summary>
internal sealed record ElementGroups(
    ILookup<string, Element> ByGroup, Dictionary<long, string> GroupByElementId);
