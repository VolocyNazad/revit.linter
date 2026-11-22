using Autodesk.Revit.DB;

namespace Revit.Linter.ElementIgnoring.Abstractions.Models;

/// <summary>
/// Provides data for a notification that entries were removed from the stored ignore list.
/// </summary>
/// <param name="document">The document whose ignore list changed.</param>
/// <param name="elementIds">
/// The elements whose entries were removed. A document-level entry carries no element, so its removal
/// reports none.
/// </param>
public sealed class IgnoreListEntriesRemovedEventArgs(
    Document document,
    IReadOnlyCollection<ElementId> elementIds) : EventArgs
{
    /// <summary>Gets the document whose ignore list changed.</summary>
    public Document Document { get; } = document;

    /// <summary>Gets the elements whose entries were removed.</summary>
    public IReadOnlyCollection<ElementId> ElementIds { get; } = elementIds;
}
