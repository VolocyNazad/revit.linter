namespace Revit.Linter.ElementDiagnostics.Diagnostics.AssociatedArray;

internal sealed class AssociatedArrayDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.AssociatedArray;

    public bool IsRelevantFor(Document document) => true;
}
