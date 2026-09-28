namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeUnused;

internal sealed class DeleteUnusedFamilySymbol : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilySymbolUnused;

    public string Value => ElementDiagnosticLocalizations.GetString("deleteUnusedFamilySymbol_fix");
    public bool Execute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
