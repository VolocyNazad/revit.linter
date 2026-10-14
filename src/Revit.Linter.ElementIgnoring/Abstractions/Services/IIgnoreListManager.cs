using Autodesk.Revit.DB;
using Revit.Linter.ElementIgnoring.Abstractions.Models;

namespace Revit.Linter.ElementIgnoring.Abstractions.Services;

/// <summary>
/// Lists and removes the ignored elements of one document.
/// </summary>
/// <remarks>
/// The list holds element-level entries addressing one element and document-level entries
/// suppressing every finding of a diagnostic. All members require a valid Revit API context.
/// The mutating members must be called inside an open transaction owned by the caller.
/// </remarks>
public interface IIgnoreListManager
{
    /// <summary>
    /// Gets every ignore entry stored in the document, including entries whose elements no longer exist.
    /// </summary>
    /// <param name="document">The document whose ignore list is read.</param>
    /// <returns>The stored entries in no particular order.</returns>
    IReadOnlyList<IgnoreListEntry> GetEntries(Document document);

    /// <summary>
    /// Removes the ignore entry of an element for the specified diagnostic.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="element">The element to stop ignoring.</param>
    /// <returns><see langword="true"/> when an entry was removed; otherwise, <see langword="false"/>.</returns>
    bool Remove(string code, Element element);

    /// <summary>
    /// Removes the document-level ignore entry for the specified diagnostic.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="document">The document to stop ignoring.</param>
    /// <returns><see langword="true"/> when an entry was removed; otherwise, <see langword="false"/>.</returns>
    bool RemoveDocument(string code, Document document);

    /// <summary>
    /// Removes the entries whose elements no longer exist in the document.
    /// </summary>
    /// <param name="document">The document whose ignore list is cleaned.</param>
    /// <returns>The number of removed entries.</returns>
    /// <remarks>
    /// Missing entries are also dropped on every write and on settled pane loads, so this stays
    /// for explicit maintenance only. Detector runs never purge: an element restored by undo keeps
    /// its identifier.
    /// </remarks>
    int RemoveMissing(Document document);

    /// <summary>
    /// Removes the entries of the document, optionally limited to one diagnostic code.
    /// </summary>
    /// <param name="document">The document whose ignore list is cleared.</param>
    /// <param name="code">The diagnostic code to clear, or <see langword="null"/> for every code.</param>
    /// <returns>The number of removed entries.</returns>
    /// <remarks>
    /// Clears element-level and document-level entries alike.
    /// </remarks>
    int Clear(Document document, string? code = null);
}
