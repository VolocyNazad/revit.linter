namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypesMissingFromCatalog;

internal sealed class FamilyTypesMissingFromCatalogDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyTypesMissingFromCatalog;

    public bool IsRelevantFor(Document document) => document.IsFamilyDocument;
}
