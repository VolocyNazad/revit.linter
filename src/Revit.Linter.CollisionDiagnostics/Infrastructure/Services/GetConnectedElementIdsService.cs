using Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;
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
                element.Document, CollisionQueryNames.ConnectedElementIds, elementId: element.Id),
            () => CollectConnectedIds(element));

    private static HashSet<long> CollectConnectedIds(Element element)
    {
        HashSet<long> connectedIds = [];
        ConnectorManager? manager = GetConnectorManager(element);
        if (manager is null) return connectedIds;

        long elementId = element.Id.Value();
        foreach (Connector connector in manager.Connectors)
        {
            if (!connector.IsConnected) continue;
            foreach (Connector reference in connector.AllRefs)
            {
                if (reference.Owner is not Element owner) continue;
                long ownerId = owner.Id.Value();
                if (ownerId != elementId) connectedIds.Add(ownerId);
            }
        }

        return connectedIds;
    }

    private static ConnectorManager? GetConnectorManager(Element element) => element switch
    {
        MEPCurve curve => curve.ConnectorManager,
        FamilyInstance instance => instance.MEPModel?.ConnectorManager,
        FabricationPart part => part.ConnectorManager,
        _ => null,
    };
}
