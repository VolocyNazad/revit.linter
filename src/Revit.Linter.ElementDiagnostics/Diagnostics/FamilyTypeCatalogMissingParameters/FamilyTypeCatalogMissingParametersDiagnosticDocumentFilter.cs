namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeCatalogMissingParameters;

internal sealed class FamilyTypeCatalogMissingParametersDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyTypeCatalogMissingParameters;

    public bool IsRelevantFor(Document document) => document.IsFamilyDocument;
}
