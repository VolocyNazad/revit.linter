namespace Revit.Linter.ElementDiagnostics.Diagnostics.ReferenceLineUnused;

internal sealed class DeleteUnusedReferenceLine : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ReferenceLineUnused;

    public string Value => ElementDiagnosticLocalizations.GetString("deleteUnusedReferenceLine_fix");
    public bool Execute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
