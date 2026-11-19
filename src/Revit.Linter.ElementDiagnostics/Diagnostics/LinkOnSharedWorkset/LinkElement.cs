namespace Revit.Linter.ElementDiagnostics.Diagnostics.LinkOnSharedWorkset;

/// <summary>Recognizes links: Revit links and linked CAD files.</summary>
/// <remarks>
/// A Revit link is a <see cref="RevitLinkInstance"/>; a linked CAD file is an
/// <see cref="ImportInstance"/> whose <c>IsLinked</c> flag is set. An embedded CAD file is model
/// content, so it belongs to the workset like any other model element and does not make the
/// workset a link workset.
/// </remarks>
internal static class LinkElement
{
    /// <summary>Returns whether the element is a link to another file.</summary>
    internal static bool IsLink(Element element) => element switch
    {
        RevitLinkInstance => true,
        ImportInstance import => IsLinked(import),
        _ => false,
    };

    // An import whose state cannot be read counts as model content, not as a link: that never
    // hides a finding and never fails the whole run on one broken element.
    private static bool IsLinked(ImportInstance import)
    {
        try
        {
            return import.IsLinked;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
