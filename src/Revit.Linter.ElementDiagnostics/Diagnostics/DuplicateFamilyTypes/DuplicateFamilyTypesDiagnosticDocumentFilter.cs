namespace Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateFamilyTypes;

internal sealed class DuplicateFamilyTypesDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DuplicateFamilyTypes;

    public bool IsRelevantFor(Document document) => document.IsFamilyDocument;
}
