namespace Revit.Linter.ElementDiagnostics.Diagnostics.LargeRadialArray;

internal sealed class DeleteLargeRadialArray : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LargeRadialArray;

    public string Value => ElementDiagnosticLocalizations.GetString("deleteArray_fix");
    public bool Execute(Element targetElement)
    {
        if (targetElement is not RadialArray) return false;
        // Capture the id first: after Delete the element wrapper is invalid
        // and even reading Id throws InvalidObjectException (verified live).
        ElementId id = targetElement.Id;
        ICollection<ElementId> deleted = targetElement.Document.Delete([id]);
        return deleted.Contains(id);
    }
}
