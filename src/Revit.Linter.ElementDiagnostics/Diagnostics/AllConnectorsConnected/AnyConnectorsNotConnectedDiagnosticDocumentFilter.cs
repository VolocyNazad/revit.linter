namespace Revit.Linter.ElementDiagnostics.Diagnostics.AllConnectorsConnected;

internal sealed class AnyConnectorsNotConnectedDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.AnyConnectorsNotConnected;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
