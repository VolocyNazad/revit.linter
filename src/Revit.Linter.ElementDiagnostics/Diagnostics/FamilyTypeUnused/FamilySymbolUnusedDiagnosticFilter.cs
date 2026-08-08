using Toolkit.Revit.Extensions;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeUnused;

internal sealed class FamilySymbolUnusedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilySymbolUnused;

    public bool IsRelevantFor(Document document, Element element) 
        => element is FamilySymbol familySymbol
        && familySymbol.Category?.Id.IsCategory(BuiltInCategory.OST_ProfileFamilies) != true;
}
