namespace Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateParameterNames;

internal sealed class DuplicateParameterNamesDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DuplicateParameterNames;

    // Document.ParameterBindings throws for a family document, and a family keeps its parameters in
    // FamilyManager, where the names are unique.
    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
