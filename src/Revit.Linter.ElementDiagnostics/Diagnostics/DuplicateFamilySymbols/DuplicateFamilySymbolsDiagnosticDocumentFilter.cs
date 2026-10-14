namespace Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateFamilySymbols;

internal sealed class DuplicateFamilySymbolsDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DuplicateFamilySymbols;

    // A family document holds the nested families loaded into it as FamilySymbol
    // elements, so duplicates are checked there just like in projects.
    public bool IsRelevantFor(Document document) => true;
}
