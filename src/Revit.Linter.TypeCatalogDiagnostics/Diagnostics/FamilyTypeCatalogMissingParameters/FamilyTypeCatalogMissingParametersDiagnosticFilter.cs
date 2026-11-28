namespace Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypeCatalogMissingParameters;

internal sealed class FamilyTypeCatalogMissingParametersDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => TypeCatalogDiagnosticIdCollector.FamilyTypeCatalogMissingParameters;

    public bool IsRelevantFor(Document document, Element element) =>
        document.IsFamilyDocument && element is Family && element.Id == document.OwnerFamily.Id;
}
