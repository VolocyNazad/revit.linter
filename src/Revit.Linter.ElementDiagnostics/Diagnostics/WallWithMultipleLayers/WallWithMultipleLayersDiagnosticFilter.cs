namespace Revit.Linter.ElementDiagnostics.Diagnostics.WallWithMultipleLayers;

internal sealed class WallWithMultipleLayersDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.WallWithMultipleLayers;

    public bool IsRelevantFor(Document document, Element element) => element is Wall;
}
