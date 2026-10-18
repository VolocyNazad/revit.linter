namespace Revit.Linter.ElementDiagnostics.Diagnostics.ArrayWithFormula;

internal sealed class ArrayWithFormulaDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ArrayWithFormula;

    public bool IsRelevantFor(Document document) => true;
}
