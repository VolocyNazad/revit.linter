namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeCatalogInUse;

internal sealed class FamilyTypeCatalogInUseDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyTypeCatalogInUse;

    public bool IsRelevantFor(Document document, Element element) =>
        document.IsFamilyDocument && element is Family && element.Id == document.OwnerFamily.Id;
}
