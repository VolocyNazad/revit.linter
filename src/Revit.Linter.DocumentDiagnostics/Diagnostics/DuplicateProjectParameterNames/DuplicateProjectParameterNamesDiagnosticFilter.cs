namespace Revit.Linter.DocumentDiagnostics.Diagnostics.DuplicateProjectParameterNames;

internal sealed class DuplicateProjectParameterNamesDiagnosticFilter : IDocumentDiagnosticFilter
{
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.DuplicateProjectParameterNames;

    // Document.ParameterBindings throws for a family document: a family keeps its parameters in
    // FamilyManager instead, and their names are unique there.
    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
