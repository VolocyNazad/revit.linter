namespace Revit.Linter.ElementDiagnostics.Diagnostics.GridNotPinned;

internal sealed class PinGrid : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.GridNotPinned;

    public string Value => ElementDiagnosticLocalizations.GetString("pinGrid_fix");

    public bool Execute(Element targetElement)
    {
        if (targetElement is not (Grid or MultiSegmentGrid)) return false;
        if (targetElement.Pinned) return true;

        try
        {
            targetElement.Pinned = true;
            return true;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException)
        {
            // Grids that cannot be pinned (for example, owned by a group) report as not applied,
            // not as a failure.
            return false;
        }
    }
}
