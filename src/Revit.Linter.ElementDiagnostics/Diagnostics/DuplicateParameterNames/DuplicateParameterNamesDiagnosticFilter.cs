namespace Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateParameterNames;

internal sealed class DuplicateParameterNamesDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DuplicateParameterNames;

    // Only a loaded family brings parameters of its own: an element of a system family has
    // built-in and project parameters, and two project parameters with one name are the document
    // check DOC004.
    public bool IsRelevantFor(Document document, Element element) => element is FamilyInstance;
}
