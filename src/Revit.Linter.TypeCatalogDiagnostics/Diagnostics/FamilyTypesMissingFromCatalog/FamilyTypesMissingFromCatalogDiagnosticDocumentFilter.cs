namespace Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypesMissingFromCatalog;

internal sealed class FamilyTypesMissingFromCatalogDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => TypeCatalogDiagnosticIdCollector.FamilyTypesMissingFromCatalog;

    public bool IsRelevantFor(Document document) => document.IsFamilyDocument;
}
