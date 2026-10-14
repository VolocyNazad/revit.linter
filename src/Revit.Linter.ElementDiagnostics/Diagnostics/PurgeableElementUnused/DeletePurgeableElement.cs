namespace Revit.Linter.ElementDiagnostics.Diagnostics.PurgeableElementUnused;

internal sealed class DeletePurgeableElement : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.PurgeableElementUnused;

    public string Value => ElementDiagnosticLocalizations.GetString("deletePurgeableElement_fix");
    public bool Execute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
