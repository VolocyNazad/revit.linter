namespace Revit.Linter.ElementDiagnostics.Diagnostics.ArrayWithFormula;

internal sealed class ArrayWithFormulaDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ArrayWithFormula;

    public bool IsRelevantFor(Document document, Element element) => element is BaseArray;
}
