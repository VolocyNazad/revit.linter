namespace Revit.Linter.ElementDiagnostics.Diagnostics.ViewHasHiddenElements;

/// <summary>
/// Finds elements that are permanently hidden in a view, the state the Reveal Hidden Elements
/// command (the light bulb on the view control bar) unmasks.
/// </summary>
/// <remarks>
/// <c>Element.IsHidden</c> covers only the element-level Hide in View command. Hiding a whole
/// category or hiding elements with a view filter is a different visibility state and is not reported.
/// The call can reject an element whose shape has no permanent hide state, so an unreadable element
/// counts as visible instead of failing the whole run.
/// </remarks>
internal static class HiddenInViewElements
{
    /// <summary>Returns whether the element is permanently hidden in the view.</summary>
    internal static bool IsHidden(Element element, View view)
    {
        try
        {
            return element.IsHidden(view);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Collects the elements that are permanently hidden in the view.</summary>
    internal static List<Element> Collect(IReadOnlyList<Element> elements, View view) =>
        elements.Where(element => IsHidden(element, view)).ToList();
}
