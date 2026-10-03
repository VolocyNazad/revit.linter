using Autodesk.Revit.DB;

namespace Revit.Linter.ElementIgnoring.Abstractions.Services;

/// <summary>
/// Determines whether elements are ignored for individual diagnostics.
/// </summary>
public interface IIgnoreElementDetector
{
    /// <summary>
    /// Determines whether an element is ignored for the specified diagnostic.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="element">The element to inspect.</param>
    /// <returns><see langword="true"/> when the element is ignored; otherwise, <see langword="false"/>.</returns>
    bool IsElementIgnored(string code, Element element);

    /// <summary>
    /// Gets the ignore information of a document for checking many elements in a row.
    /// </summary>
    /// <param name="document">The document whose elements will be checked.</param>
    /// <returns>The ignore information valid until the document changes.</returns>
    /// <remarks>
    /// Prefer this over <see cref="IsElementIgnored"/> in a loop: the document is resolved once instead
    /// of once per element.
    /// </remarks>
    IIgnoredElements GetIgnoredElements(Document document);
}
