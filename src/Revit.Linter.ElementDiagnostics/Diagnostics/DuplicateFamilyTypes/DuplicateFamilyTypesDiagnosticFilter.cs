namespace Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateFamilyTypes;

internal sealed class DuplicateFamilyTypesDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DuplicateFamilyTypes;

    // Nested loaded families have their own types, invisible to the host FamilyManager:
    // only the owning family is checked.
    public bool IsRelevantFor(Document document, Element element) =>
        element is Family && element.Id == document.OwnerFamily.Id;
}
