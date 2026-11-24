namespace Revit.Linter.ElementDiagnostics.Diagnostics.UnsharedFamilyParameters;

internal sealed class UnsharedFamilyParametersDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.UnsharedFamilyParameters;

    // Family parameters exist only in a family document: a project keeps project parameters instead.
    public bool IsRelevantFor(Document document) => document.IsFamilyDocument;
}
