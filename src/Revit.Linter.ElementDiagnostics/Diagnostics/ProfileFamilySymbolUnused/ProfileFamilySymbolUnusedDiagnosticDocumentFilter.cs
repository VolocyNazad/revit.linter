namespace Revit.Linter.ElementDiagnostics.Diagnostics.ProfileFamilySymbolUnused;

internal sealed class ProfileFamilySymbolUnusedDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ProfileFamilySymbolUnused;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
