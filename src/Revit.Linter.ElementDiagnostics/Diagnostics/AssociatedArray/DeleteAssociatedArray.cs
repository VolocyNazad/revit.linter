namespace Revit.Linter.ElementDiagnostics.Diagnostics.AssociatedArray;

internal sealed class DeleteAssociatedArray : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.AssociatedArray;

    public string Value => ElementDiagnosticLocalizations.GetString("deleteArray_fix");
    public bool Execute(Element targetElement)
    {
        if (targetElement is not BaseArray) return false;
        // Capture the id first: after Delete the element wrapper is invalid
        // and even reading Id throws InvalidObjectException (verified live).
        ElementId id = targetElement.Id;
        ICollection<ElementId> deleted = targetElement.Document.Delete([id]);
        return deleted.Contains(id);
    }
}
