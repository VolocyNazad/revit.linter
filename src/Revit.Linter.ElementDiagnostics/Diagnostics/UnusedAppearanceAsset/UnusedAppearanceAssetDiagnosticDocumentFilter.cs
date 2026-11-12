namespace Revit.Linter.ElementDiagnostics.Diagnostics.UnusedAppearanceAsset;

internal sealed class UnusedAppearanceAssetDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.UnusedAppearanceAsset;

    public bool IsRelevantFor(Document document) => true;
}
