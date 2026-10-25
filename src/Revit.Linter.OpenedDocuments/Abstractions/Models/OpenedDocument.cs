namespace Revit.Linter.OpenedDocuments.Abstractions.Models;

/// <summary>
/// Describes an opened Revit document as an item of the shared document selector.
/// </summary>
/// <remarks>
/// The selector is expressed as this stable model rather than as a view model, so a presenter can show the
/// documents of the session without depending on the selector implementation.
/// </remarks>
public sealed class OpenedDocument
{
    /// <summary>
    /// Gets the Revit document title used to identify the selected document.
    /// </summary>
    /// <remarks>An empty title is the selector entry that represents every document.</remarks>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the localized text displayed in the document selector.
    /// </summary>
    public required string DisplayName { get; init; }
}
