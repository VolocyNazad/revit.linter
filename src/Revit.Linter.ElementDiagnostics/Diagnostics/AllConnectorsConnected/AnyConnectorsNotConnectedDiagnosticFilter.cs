namespace Revit.Linter.ElementDiagnostics.Diagnostics.AllConnectorsConnected;

internal sealed class AnyConnectorsNotConnectedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.AnyConnectorsNotConnected;

    public bool IsRelevantFor(Document document, Element element) => element switch
    {
        MEPCurve => true,
        FamilyInstance familyInstance => familyInstance.MEPModel?.ConnectorManager is not null,
        _ => false,
    };
}
