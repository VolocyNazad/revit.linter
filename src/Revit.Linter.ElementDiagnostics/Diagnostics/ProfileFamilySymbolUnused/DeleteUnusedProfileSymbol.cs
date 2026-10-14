namespace Revit.Linter.ElementDiagnostics.Diagnostics.ProfileFamilySymbolUnused;

internal sealed class DeleteUnusedProfileSymbol : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ProfileFamilySymbolUnused;

    public string Value => ElementDiagnosticLocalizations.GetString("deleteUnusedProfileSymbol_fix");
    public bool Execute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
