using Revit.Sugar;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyUnused;

internal sealed class FamilyUnusedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyUnused;

    public bool IsRelevantFor(Document document, Element element)
        => element is Family family
        && family.FamilyCategory?.Id.IsCategory(BuiltInCategory.OST_ProfileFamilies) != true
        && !IsEditedFamily(document, family);

    // The edited family itself can never have instances inside its own document,
    // so reporting it as unused is always a false positive.
    private static bool IsEditedFamily(Document document, Family family)
        => document.IsFamilyDocument && document.OwnerFamily is { } owner && owner.Id == family.Id;
}
