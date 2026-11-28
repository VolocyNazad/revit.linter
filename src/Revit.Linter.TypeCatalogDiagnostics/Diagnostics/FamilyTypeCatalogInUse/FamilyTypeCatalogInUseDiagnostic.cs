using Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypeCatalog;

namespace Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypeCatalogInUse;

internal sealed class FamilyTypeCatalogInUseDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => TypeCatalogDiagnosticIdCollector.FamilyTypeCatalogInUse;

    public IReadOnlyList<DiagnosticFeedback> Execute(Document document, View? view, Element targetElement)
    {
        if (!document.IsFamilyDocument || targetElement is not Family || targetElement.Id != document.OwnerFamily.Id)
            return DiagnosticFeedbacks.Valid;
        if (FamilyTypeCatalogReader.TryRead(document) is not { TypeNames.Count: > 0 } catalog)
            return DiagnosticFeedbacks.Valid;

        // Revit cannot delete the last type of a family, so one type always remains and is not a
        // second storage approach. Only a type beyond it means the family keeps its own set next to
        // the catalog.
        int ownTypeCount = Math.Max(0, document.FamilyManager.Types.Size - 1);
        if (ownTypeCount == 0) return DiagnosticFeedbacks.Valid;

        return DiagnosticFeedbacks.Of(new(DiagnosticVerdict.NotValid,
            new Dictionary<string, object>
            {
                ["familyName"] = FamilyTypeCatalogReader.ReadFamilyName(document),
                ["catalogName"] = catalog.FileName,
                ["ownTypeCount"] = ownTypeCount,
                ["catalogTypeCount"] = catalog.TypeNames.Count,
            }));
    }
}
