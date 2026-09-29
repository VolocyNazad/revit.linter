namespace Revit.Linter.OpenedDocuments.ViewModels;

/// <summary>
/// Describes an opened Revit document as an item in a document selector.
/// </summary>
public sealed class DocumentViewModel
{
    /// <summary>
    /// Gets the Revit document title used to identify the selected document.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the localized text displayed in the document selector.
    /// </summary>
    public required string DisplayName { get; init; }
}
