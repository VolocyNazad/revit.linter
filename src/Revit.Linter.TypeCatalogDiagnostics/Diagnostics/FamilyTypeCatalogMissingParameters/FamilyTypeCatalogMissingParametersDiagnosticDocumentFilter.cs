namespace Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypeCatalogMissingParameters;

internal sealed class FamilyTypeCatalogMissingParametersDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => TypeCatalogDiagnosticIdCollector.FamilyTypeCatalogMissingParameters;

    public bool IsRelevantFor(Document document) => document.IsFamilyDocument;
}
