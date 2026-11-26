namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypesMissingFromCatalog;

internal sealed class FamilyTypesMissingFromCatalogDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyTypesMissingFromCatalog;

    public bool IsRelevantFor(Document document, Element element) =>
        document.IsFamilyDocument && element is Family && element.Id == document.OwnerFamily.Id;
}
