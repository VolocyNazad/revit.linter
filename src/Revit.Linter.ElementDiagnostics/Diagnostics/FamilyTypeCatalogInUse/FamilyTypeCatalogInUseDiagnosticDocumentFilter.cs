namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeCatalogInUse;

internal sealed class FamilyTypeCatalogInUseDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyTypeCatalogInUse;

    public bool IsRelevantFor(Document document) => document.IsFamilyDocument;
}
