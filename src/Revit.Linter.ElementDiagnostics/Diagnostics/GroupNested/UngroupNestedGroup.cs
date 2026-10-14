namespace Revit.Linter.ElementDiagnostics.Diagnostics.GroupNested;

internal sealed class UngroupNestedGroup : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.GroupNested;

    public string Value => ElementDiagnosticLocalizations.GetString("ungroupNestedGroup_fix");
    public bool Execute(Element targetElement)
    {
        if (targetElement is not Group group) return false;
        try
        {
            // UngroupMembers releases the members to the parent group. A leftover empty
            // shell is deleted so no nested group remains.
            group.UngroupMembers();
            if (group.IsValidObject && group.GetMemberIds().Count == 0)
                group.Document.Delete(group.Id);
            return true;
        }
        catch (InvalidOperationException)
        {
            // Groups that cannot be ungrouped report as not applied, not as a failure.
            return false;
        }
    }
}
