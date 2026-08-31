namespace Revit.Linter.ElementDiagnostics.Diagnostics.DetailCurveExists;

internal sealed class DetailCurveExistsDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DetailCurveExists;

    public bool IsRelevantFor(Document document) => true;
}
