namespace Revit.Linter.ElementDiagnostics.Diagnostics.ReferencePlaneUnused;

internal sealed class DeleteUnusedReferencePlane : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ReferencePlaneUnused;

    public string Value => ElementDiagnosticLocalizations.GetString("deleteUnusedReferencePlane_fix");
    public bool Execute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
