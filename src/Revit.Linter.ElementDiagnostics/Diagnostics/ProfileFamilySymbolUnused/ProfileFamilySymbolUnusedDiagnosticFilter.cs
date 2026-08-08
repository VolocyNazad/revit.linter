using Toolkit.Revit.Extensions;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.ProfileFamilySymbolUnused;

internal sealed class ProfileFamilySymbolUnusedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ProfileFamilySymbolUnused;

    public bool IsRelevantFor(Document document, Element element)
        => element is FamilySymbol symbol
           && symbol.Category?.Id.IsCategory(BuiltInCategory.OST_ProfileFamilies) == true;
}
