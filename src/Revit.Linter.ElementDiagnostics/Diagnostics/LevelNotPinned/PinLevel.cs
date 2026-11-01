namespace Revit.Linter.ElementDiagnostics.Diagnostics.LevelNotPinned;

internal sealed class PinLevel : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LevelNotPinned;

    public string Value => ElementDiagnosticLocalizations.GetString("pinLevel_fix");

    public bool Execute(Element targetElement)
    {
        if (targetElement is not Level level) return false;
        if (level.Pinned) return true;

        try
        {
            level.Pinned = true;
            return true;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException)
        {
            // Levels that cannot be pinned (for example, owned by a group) report as not
            // applied, not as a failure.
            return false;
        }
    }
}
