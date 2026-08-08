using Toolkit.Revit.Extensions;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyUnused;

internal sealed class FamilyUnusedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyUnused;

    public bool IsRelevantFor(Document document, Element element) 
        => element is Family family
        && family.FamilyCategory?.Id.IsCategory(BuiltInCategory.OST_ProfileFamilies) != true;
}
