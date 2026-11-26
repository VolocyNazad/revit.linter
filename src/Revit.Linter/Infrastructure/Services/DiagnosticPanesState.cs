namespace Revit.Linter.Infrastructure.Services;

/// <summary>Holds which diagnostics panes were open before Revit switched to another document.</summary>
/// <remarks>
/// Revit hides the add-in panes itself when the last document closes. That state belongs to Revit and
/// says nothing about the user's choice, so it does not replace the remembered one; every other state
/// is the user's. The type stays free of Revit API calls so the decision is testable without Revit.
/// </remarks>
internal sealed class DiagnosticPanesState
{
    private bool[]? _shown;

    /// <summary>Gets the remembered panes, or <see langword="null"/> when nothing was remembered yet.</summary>
    public IReadOnlyList<bool>? Shown => _shown;

    /// <summary>Stores the panes that are open now, unless Revit hid them because no document is open.</summary>
    /// <param name="shown">The panes that are open now, in the order of the pane list.</param>
    /// <param name="documentIsOpen">Whether a document is open at the time of the switch.</param>
    public void Remember(IReadOnlyList<bool> shown, bool documentIsOpen)
    {
        if (!documentIsOpen && !AnyShown(shown)) return;
        _shown = [.. shown];
    }

    private static bool AnyShown(IReadOnlyList<bool> shown)
    {
        for (int index = 0; index < shown.Count; index++)
            if (shown[index]) return true;

        return false;
    }
}
