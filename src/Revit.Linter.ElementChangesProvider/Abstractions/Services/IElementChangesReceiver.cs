using Revit.Linter.ElementChangesProvider.Abstractions.Models;

namespace Revit.Linter.ElementChangesProvider.Abstractions.Services;

/// <summary>
/// Exposes notifications about reported element changes.
/// </summary>
public interface IElementChangesReceiver
{
    /// <summary>
    /// Occurs when a set of element changes is reported.
    /// </summary>
    event ElementChangesHandler? Sent;
}
