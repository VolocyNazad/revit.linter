namespace Revit.Linter.ElementDiagnostics.Diagnostics.GroupExists;

internal sealed class UngroupGroup : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.GroupExists;

    public string Value => ElementDiagnosticLocalizations.GetString("ungroupGroup_fix");
    public bool Execute(Element targetElement)
    {
        if (targetElement is not Group group) return false;
        try
        {
            // UngroupMembers dissolves the group and keeps its members.
            group.UngroupMembers();
            return true;
        }
        catch (InvalidOperationException)
        {
            // Groups that cannot be ungrouped (for example, attached to design options)
            // report as not applied, not as a failure.
            return false;
        }
    }
}
