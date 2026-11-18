using Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;
using Revit.Linter.DocumentQueries;
using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Sugar;

namespace Revit.Linter.CollisionDiagnostics.Infrastructure.Services;

// Connector references are read-only, so collecting them never leaves a transaction behind.
internal sealed class GetConnectedElementIdsService(IDocumentQueryService documentQueries)
    : IGetConnectedElementIdsService
{
    public HashSet<long> Execute(Element element)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(
                element.Document, CollisionQuery.ConnectedElementIds.CacheName(), elementId: element.Id),
            () => CollectConnectedIds(element));

    private static HashSet<long> CollectConnectedIds(Element element) =>
        [.. MepConnections.GetConnectedElements(element).Select(connected => connected.Id.Value())];
}
