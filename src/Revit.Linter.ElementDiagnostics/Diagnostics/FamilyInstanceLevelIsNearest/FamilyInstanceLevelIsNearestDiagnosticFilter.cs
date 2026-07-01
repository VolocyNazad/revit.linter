namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyInstanceLevelIsNearest;

internal sealed class FamilyInstanceLevelIsNearestDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyInstanceLevelIsNearest;

    public bool IsRelevantFor(Document document, Element element) => element is FamilyInstance { Host: null };
}
