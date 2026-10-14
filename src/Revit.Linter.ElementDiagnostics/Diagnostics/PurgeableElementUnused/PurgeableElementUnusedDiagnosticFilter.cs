namespace Revit.Linter.ElementDiagnostics.Diagnostics.PurgeableElementUnused;

internal sealed class PurgeableElementUnusedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.PurgeableElementUnused;

    public bool IsRelevantFor(Document document, Element element)
        => element is not (Family or FamilySymbol or Material);
}
