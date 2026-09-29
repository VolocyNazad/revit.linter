namespace Revit.Linter.ElementChangesProvider.Abstractions.Models;

/// <summary>
/// Provides data for an element-changes notification.
/// </summary>
/// <param name="changes">The reported element changes.</param>
public sealed class ElementChangesSentEventArgs(ElementChanges changes) : EventArgs
{
    /// <summary>
    /// Gets the reported element changes.
    /// </summary>
    public ElementChanges Changes { get; } = changes;
}
