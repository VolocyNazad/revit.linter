namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyInstanceElevationIsTolerant;

internal sealed class FamilyInstanceElevationIsTolerantDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyInstanceElevationIsTolerant;

    public bool IsRelevantFor(Document document, Element element) => element is FamilyInstance;
}