using Autodesk.Revit.DB;
using Revit.Linter.ElementIgnoring.Abstractions.Models;

namespace Revit.Linter.ElementIgnoring.Abstractions.Services;

/// <summary>
/// Marks elements as ignored for individual diagnostics.
/// </summary>
/// <remarks>
/// Only the host document can be ignored; document-level ignores are stored in the document itself.
/// Every member requires a valid Revit API context and must be called inside an open
/// transaction owned by the caller.
/// </remarks>
public interface IIgnoreElementProvider
{
    /// <summary>
    /// Marks an element as ignored for the specified diagnostic.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="element">The element to ignore.</param>
    /// <returns>The outcome of the request.</returns>
    IgnoreElementFeedback Ignore(string code, Element element);

    /// <summary>
    /// Marks several elements as ignored for the specified diagnostic in one storage update.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="elements">The elements to ignore, all of one document.</param>
    /// <returns>The outcome of the request.</returns>
    /// <remarks>
    /// The update is atomic: when any element cannot be ignored, nothing is written.
    /// </remarks>
    IgnoreElementFeedback IgnoreMany(string code, IEnumerable<Element> elements);

    /// <summary>
    /// Marks a document as ignored for the specified diagnostic.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="document">The document to ignore.</param>
    /// <returns>The outcome of the request.</returns>
    /// <remarks>
    /// A document-level entry suppresses every finding of the diagnostic in the document,
    /// whether the finding addresses an element or the document itself.
    /// </remarks>
    IgnoreElementFeedback IgnoreDocument(string code, Document document);
}
