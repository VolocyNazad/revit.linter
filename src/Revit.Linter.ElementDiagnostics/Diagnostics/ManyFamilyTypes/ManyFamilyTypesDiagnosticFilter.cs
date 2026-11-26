namespace Revit.Linter.ElementDiagnostics.Diagnostics.ManyFamilyTypes;

internal sealed class ManyFamilyTypesDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ManyFamilyTypes;

    public bool IsRelevantFor(Document document, Element element) =>
        document.IsFamilyDocument && element is Family && element.Id == document.OwnerFamily.Id;
}
