using Revit.Linter.ElementChangesProvider.Abstractions.Models;

namespace Revit.Linter.ElementChangesProvider.Abstractions.Services;

/// <summary>
/// Publishes element changes to registered receivers.
/// </summary>
public interface IElementChangesSender
{
    /// <summary>
    /// Publishes the specified element changes.
    /// </summary>
    /// <param name="changes">The element changes to publish.</param>
    void Send(ElementChanges changes);
}
