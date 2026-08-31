namespace Revit.Linter.ElementDiagnostics.Diagnostics.MaterialUnused;

internal sealed class DeleteUnusedMaterial : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.MaterialUnused;

    public string Value => "Удалить неиспользуемый материал";
    public bool Execute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}