namespace Revit.Linter.ElementDiagnostics.Diagnostics.ModelCurveExists;

internal sealed class ModelCurveExistsDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ModelCurveExists;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
