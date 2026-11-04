namespace Revit.Linter.ElementDiagnostics.Diagnostics.RevitLinkNotPinned;

internal sealed class PinRevitLink : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RevitLinkNotPinned;

    public string Value => ElementDiagnosticLocalizations.GetString("pinRevitLink_fix");

    public bool Execute(Element targetElement)
    {
        if (targetElement is not RevitLinkInstance link) return false;
        if (link.Pinned) return true;

        try
        {
            link.Pinned = true;
            return true;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException)
        {
            // Links that cannot be pinned (for example, not editable in a workshared document)
            // report as not applied, not as a failure.
            return false;
        }
    }
}
