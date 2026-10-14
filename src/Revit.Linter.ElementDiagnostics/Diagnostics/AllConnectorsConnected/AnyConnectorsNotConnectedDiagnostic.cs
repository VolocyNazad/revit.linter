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
            if (!IsCheckableConnector(connector.ConnectorType)) continue;
            if (!connector.IsConnected) return new(DiagnosticVerdict.NotValid);
        }

        return new(DiagnosticVerdict.Valid);
    }

    /// <summary>Whether the diagnostic reads connection status for this connector type.</summary>
    /// <remarks>
    /// <see cref="Autodesk.Revit.DB.Connector.IsConnected"/> throws for anything but end and physical
    /// connectors (logical, curve and reference included), so the rest are skipped as clean instead of
    /// failing the whole run.
    /// </remarks>
    internal static bool IsCheckableConnector(ConnectorType connectorType) =>
        connectorType == ConnectorType.End || connectorType == ConnectorType.Physical;
}
