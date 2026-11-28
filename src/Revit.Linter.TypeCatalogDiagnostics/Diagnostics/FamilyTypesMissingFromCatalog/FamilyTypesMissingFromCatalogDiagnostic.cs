using Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypeCatalog;

namespace Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypesMissingFromCatalog;

internal sealed class FamilyTypesMissingFromCatalogDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => TypeCatalogDiagnosticIdCollector.FamilyTypesMissingFromCatalog;

    public IReadOnlyList<DiagnosticFeedback> Execute(Document document, View? view, Element targetElement)
    {
        if (!document.IsFamilyDocument || targetElement is not Family || targetElement.Id != document.OwnerFamily.Id)
            return DiagnosticFeedbacks.Valid;
        if (FamilyTypeCatalogReader.TryRead(document) is not { TypeNames.Count: > 0 } catalog)
            return DiagnosticFeedbacks.Valid;

        // A catalog lists every type of the family by name, including the type named after the
        // family. A type the catalog does not list exists only in the family; a type with a blank
        // name has nothing to match.
        SortedSet<string> missing = new(
            FamilyTypeCatalogReader.ReadTypeNames(document).Where(name => !catalog.Contains(name)),
            StringComparer.Ordinal);

        if (missing.Count == 0) return DiagnosticFeedbacks.Valid;

        return DiagnosticFeedbacks.Of(new(DiagnosticVerdict.NotValid,
            new Dictionary<string, object>
            {
                ["familyName"] = FamilyTypeCatalogReader.ReadFamilyName(document),
                ["catalogName"] = catalog.FileName,
                ["typeNames"] = string.Join(", ", missing),
            }));
    }
}
