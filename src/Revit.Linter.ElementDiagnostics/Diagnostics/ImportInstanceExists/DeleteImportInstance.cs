namespace Revit.Linter.ElementDiagnostics.Diagnostics.ImportInstanceExists;

internal sealed class DeleteImportInstance : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ImportInstanceExists;

    public string Value => ElementDiagnosticLocalizations.GetString("deleteImportInstance_fix");
    public bool Execute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
