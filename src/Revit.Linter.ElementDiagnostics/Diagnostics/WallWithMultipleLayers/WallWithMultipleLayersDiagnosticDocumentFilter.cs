namespace Revit.Linter.ElementDiagnostics.Diagnostics.WallWithMultipleLayers;

internal sealed class WallWithMultipleLayersDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.WallWithMultipleLayers;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
