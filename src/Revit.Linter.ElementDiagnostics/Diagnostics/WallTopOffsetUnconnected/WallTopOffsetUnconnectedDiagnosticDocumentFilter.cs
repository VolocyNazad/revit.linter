namespace Revit.Linter.ElementDiagnostics.Diagnostics.WallTopOffsetUnconnected;

internal sealed class WallTopOffsetUnconnectedDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.WallTopOffsetUnconnected;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
