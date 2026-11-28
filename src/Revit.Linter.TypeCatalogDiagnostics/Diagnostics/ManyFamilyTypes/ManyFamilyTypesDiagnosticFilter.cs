namespace Revit.Linter.TypeCatalogDiagnostics.Diagnostics.ManyFamilyTypes;

internal sealed class ManyFamilyTypesDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => TypeCatalogDiagnosticIdCollector.ManyFamilyTypes;

    public bool IsRelevantFor(Document document, Element element) =>
        document.IsFamilyDocument && element is Family && element.Id == document.OwnerFamily.Id;
}
