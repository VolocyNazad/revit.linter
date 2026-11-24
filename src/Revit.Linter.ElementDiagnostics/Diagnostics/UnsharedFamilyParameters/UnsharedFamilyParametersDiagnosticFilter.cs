namespace Revit.Linter.ElementDiagnostics.Diagnostics.UnsharedFamilyParameters;

internal sealed class UnsharedFamilyParametersDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.UnsharedFamilyParameters;

    // Nested loaded families own their parameters, invisible to the host FamilyManager: only the
    // owning family is checked.
    public bool IsRelevantFor(Document document, Element element) =>
        element is Family && element.Id == document.OwnerFamily.Id;
}
