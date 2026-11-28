namespace Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypeCatalogInUse;

internal sealed class FamilyTypeCatalogInUseDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => TypeCatalogDiagnosticIdCollector.FamilyTypeCatalogInUse;

    public bool IsRelevantFor(Document document, Element element) =>
        document.IsFamilyDocument && element is Family && element.Id == document.OwnerFamily.Id;
}
