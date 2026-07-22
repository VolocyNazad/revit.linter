namespace Revit.Linter.ElementChangesProvider.Abstractions.Models;

public sealed class ElementChangesSentEventArgs(ElementChanges changes) : EventArgs
{
    public ElementChanges Changes { get; } = changes;
}
