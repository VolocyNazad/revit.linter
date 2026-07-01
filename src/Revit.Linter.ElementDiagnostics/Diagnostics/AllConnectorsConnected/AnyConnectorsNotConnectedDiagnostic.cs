namespace Revit.Linter.ElementDiagnostics.Diagnostics.AllConnectorsConnected;

internal sealed class AnyConnectorsNotConnectedDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.AnyConnectorsNotConnected;

    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
    {
        ConnectorManager? connectorManager = targetElement switch
        {
            MEPCurve mepCurve => mepCurve.ConnectorManager,
            FamilyInstance familyInstance => familyInstance.MEPModel.ConnectorManager,
            _ => throw new InvalidOperationException($"Element {targetElement.Id} of type {targetElement.GetType()} does not support connectors."),
        };

        if (connectorManager is null) return new(DiagnosticVerdict.Valid);
        foreach (Connector connector in connectorManager.Connectors)
        {
            if (connector.ConnectorType != ConnectorType.Physical) continue; //todo Без этого не работало иногда. Убедиться, что все ок.
            if (!connector.IsConnected) return new(DiagnosticVerdict.NotValid);
        }

        return new(DiagnosticVerdict.Valid);
    }
}
