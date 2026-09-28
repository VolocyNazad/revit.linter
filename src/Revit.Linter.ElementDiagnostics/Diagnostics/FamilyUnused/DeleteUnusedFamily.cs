namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyUnused;

internal sealed class DeleteUnusedFamily : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyUnused;

    public string Value => ElementDiagnosticLocalizations.GetString("deleteUnusedFamily_fix");
    public bool Execute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
