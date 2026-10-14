namespace Revit.Linter.ElementDiagnostics.Diagnostics.UnfilledFamilyParameters;

internal sealed class UnfilledFamilyParametersDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.UnfilledFamilyParameters;

    public bool IsRelevantFor(Document document) => document.IsFamilyDocument;
}
