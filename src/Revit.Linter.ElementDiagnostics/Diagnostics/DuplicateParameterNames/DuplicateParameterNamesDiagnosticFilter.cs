namespace Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateParameterNames;

internal sealed class DuplicateParameterNamesDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DuplicateParameterNames;

    // Only a loaded family brings parameters of its own: an element of a system family has
    // built-in and project parameters, and two project parameters with one name are the document
    // check DOC004. A system family type is not a FamilySymbol, which separates the two without
    // opening anything; an in-place family keeps its parameters on the instance.
    public bool IsRelevantFor(Document document, Element element) =>
        element is Family family
        && (family.IsInPlace
            || family.GetFamilySymbolIds().Any(id => document.GetElement(id) is FamilySymbol));
}
