namespace Revit.Linter.ElementDiagnostics.Diagnostics.UnusedAppearanceAsset;

internal sealed class DeleteUnusedAppearanceAsset : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.UnusedAppearanceAsset;

    public string Value => ElementDiagnosticLocalizations.GetString("deleteUnusedAppearanceAsset_fix");

    public bool Execute(Element targetElement)
    {
        Document document = targetElement.Document;
        ElementId elementId = targetElement.Id;
        // Delete throws for an element the document does not allow to delete, so ask first.
        return DocumentValidation.CanDeleteElement(document, elementId)
            && document.Delete(elementId).Any();
    }
}
