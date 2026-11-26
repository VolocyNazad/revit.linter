namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeCatalogMissingParameters;

internal sealed class FamilyTypeCatalogMissingParametersDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyTypeCatalogMissingParameters;

    public bool IsRelevantFor(Document document, Element element) =>
        document.IsFamilyDocument && element is Family && element.Id == document.OwnerFamily.Id;
}
