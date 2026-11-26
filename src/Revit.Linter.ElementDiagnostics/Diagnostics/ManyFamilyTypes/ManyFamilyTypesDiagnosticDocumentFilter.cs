namespace Revit.Linter.ElementDiagnostics.Diagnostics.ManyFamilyTypes;

internal sealed class ManyFamilyTypesDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ManyFamilyTypes;

    public bool IsRelevantFor(Document document) => document.IsFamilyDocument;
}
