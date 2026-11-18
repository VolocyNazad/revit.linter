namespace Revit.Linter.DocumentQueries;

/// <summary>Reads the elements joined to an element through its MEP connectors.</summary>
/// <remarks>
/// <see cref="Autodesk.Revit.DB.Connector.AllRefs"/> answers for every connector type, while
/// <see cref="Autodesk.Revit.DB.Connector.IsConnected"/> throws for a logical connector, and electrical
/// equipment, wires and systems connect logically. The references of a connector that is not connected
/// point back to its own element, so the owner filter leaves other elements only.
/// </remarks>
public static class MepConnections
{
    /// <summary>Enumerates the elements joined to <paramref name="element"/> through its MEP connectors.</summary>
    /// <param name="element">The element whose connectors are read.</param>
    /// <returns>
    /// Every other element reachable through the connectors of <paramref name="element"/>, each once and in
    /// connector order; empty for an element without connectors or without connections.
    /// </returns>
    public static IEnumerable<Element> GetConnectedElements(Element element)
    {
        ConnectorManager? manager = GetConnectorManager(element);
        if (manager is null) yield break;

        ElementId elementId = element.Id;
        HashSet<ElementId> visited = [];
        foreach (Connector connector in manager.Connectors)
        {
            foreach (Connector reference in connector.AllRefs)
            {
                if (reference.Owner is not { } owner || owner.Id == elementId) continue;
                if (visited.Add(owner.Id)) yield return owner;
            }
        }
    }

    private static ConnectorManager? GetConnectorManager(Element element) => element switch
    {
        MEPCurve curve => curve.ConnectorManager,
        FamilyInstance instance => instance.MEPModel?.ConnectorManager,
        FabricationPart part => part.ConnectorManager,
        _ => null,
    };
}
