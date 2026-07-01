namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeUnused;

internal sealed class FamilySymbolUnusedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilySymbolUnused;

    public bool IsRelevantFor(Document document, Element element) 
        => element is FamilySymbol familySymbol
        // todo С профилями проблемы (не понятно как проверять их использование. Например в импостах витражей)
        && familySymbol.Category?.Id.IntegerValue != (int)BuiltInCategory.OST_ProfileFamilies;
}