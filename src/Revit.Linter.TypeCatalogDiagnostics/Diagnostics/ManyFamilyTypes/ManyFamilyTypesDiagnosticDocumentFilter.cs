namespace Revit.Linter.TypeCatalogDiagnostics.Diagnostics.ManyFamilyTypes;

internal sealed class ManyFamilyTypesDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => TypeCatalogDiagnosticIdCollector.ManyFamilyTypes;

    public bool IsRelevantFor(Document document) => document.IsFamilyDocument;
}
