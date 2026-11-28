namespace Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypeCatalogInUse;

internal sealed class FamilyTypeCatalogInUseDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => TypeCatalogDiagnosticIdCollector.FamilyTypeCatalogInUse;

    public bool IsRelevantFor(Document document) => document.IsFamilyDocument;
}
