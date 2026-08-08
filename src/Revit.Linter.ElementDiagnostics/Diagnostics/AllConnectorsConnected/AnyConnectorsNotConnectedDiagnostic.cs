namespace Revit.Linter.ElementDiagnostics.Diagnostics.AllConnectorsConnected;

internal sealed class AnyConnectorsNotConnectedDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.AnyConnectorsNotConnected;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        ConnectorManager? connectorManager = targetElement switch
        {
            MEPCurve mepCurve => mepCurve.ConnectorManager,
            FamilyInstance familyInstance => familyInstance.MEPModel?.ConnectorManager,
            _ => null,
        };

        if (connectorManager is null) return new(DiagnosticVerdict.Valid);
        foreach (Connector connector in connectorManager.Connectors)
        {
            if (connector.ConnectorType == ConnectorType.Logical) continue;
            if (!connector.IsConnected) return new(DiagnosticVerdict.NotValid);
        }

        return new(DiagnosticVerdict.Valid);
    }
}
