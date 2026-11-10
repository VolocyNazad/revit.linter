namespace Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateMaterial;

internal sealed class DuplicateMaterialDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DuplicateMaterial;

    // Materials exist in both projects and families, so the check has nothing to narrow.
    public bool IsRelevantFor(Document document) => true;
}
