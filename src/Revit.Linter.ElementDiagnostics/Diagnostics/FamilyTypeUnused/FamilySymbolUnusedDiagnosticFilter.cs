using Revit.Sugar;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeUnused;

internal sealed class FamilySymbolUnusedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilySymbolUnused;

    public bool IsRelevantFor(Document document, Element element)
        => element is FamilySymbol familySymbol
        && familySymbol.Category?.Id.IsCategory(BuiltInCategory.OST_ProfileFamilies) != true
        && !IsEditedFamilySymbol(document, familySymbol);

    // The edited family's own types can never have instances inside their own document,
    // so reporting them as unused is always a false positive.
    private static bool IsEditedFamilySymbol(Document document, FamilySymbol familySymbol)
        => document.IsFamilyDocument
        && document.OwnerFamily is { } owner
        && familySymbol.Family.Id == owner.Id;
}
