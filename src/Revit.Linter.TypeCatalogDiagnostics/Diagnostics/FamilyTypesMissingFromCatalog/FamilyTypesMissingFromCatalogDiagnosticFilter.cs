namespace Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypesMissingFromCatalog;

internal sealed class FamilyTypesMissingFromCatalogDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => TypeCatalogDiagnosticIdCollector.FamilyTypesMissingFromCatalog;

    public bool IsRelevantFor(Document document, Element element) =>
        document.IsFamilyDocument && element is Family && element.Id == document.OwnerFamily.Id;
}
