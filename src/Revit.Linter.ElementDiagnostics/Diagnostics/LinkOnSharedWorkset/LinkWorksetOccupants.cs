using System.Globalization;
using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.ElementDiagnostics.Infrastructure.Extensions;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.LinkOnSharedWorkset;

/// <summary>
/// Collects the elements that share a workset with a link, that is everything a link workset must
/// not hold.
/// </summary>
/// <remarks>
/// Links themselves are excluded, so a workset with two links and nothing else stays clean. Views
/// and element types are excluded too: they are not model content of the workset, and the link type
/// of the checked link would otherwise turn every link into a finding. The result is cached per
/// workset and ordered by identifier, so the message and the dependency list stay the same on every
/// run however many links share the workset.
/// </remarks>
internal static class LinkWorksetOccupants
{
    private const string OccupantsQuery = "element-diagnostics:link-workset-occupants";

    internal static IReadOnlyList<Element> Get(
        IDocumentQueryService documentQueries, Document document, WorksetId worksetId)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(
                document,
                OccupantsQuery,
                argument: worksetId.IntegerValue.ToString(CultureInfo.InvariantCulture)),
            () => Collect(document, worksetId));

    private static Element[] Collect(Document document, WorksetId worksetId) =>
        new FilteredElementCollector(document)
            .WherePasses(new ElementWorksetFilter(worksetId))
            .WhereElementIsNotElementType()
            .ToElements()
            .Where(element => element is not View && !LinkElement.IsLink(element))
            .OrderBy(element => element.Id.IdValue())
            .ToArray();
}
