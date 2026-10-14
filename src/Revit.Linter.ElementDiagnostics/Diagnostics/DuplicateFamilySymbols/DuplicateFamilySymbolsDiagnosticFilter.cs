namespace Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateFamilySymbols;

internal sealed class DuplicateFamilySymbolsDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DuplicateFamilySymbols;

    public bool IsRelevantFor(Document document, Element element) => element is FamilySymbol;
}
