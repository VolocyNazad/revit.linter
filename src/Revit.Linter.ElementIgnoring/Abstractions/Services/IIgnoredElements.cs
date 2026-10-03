using Autodesk.Revit.DB;

namespace Revit.Linter.ElementIgnoring.Abstractions.Services;

/// <summary>
/// Answers which elements of one document are ignored for which diagnostics.
/// </summary>
/// <remarks>
/// Obtained from <see cref="IIgnoreElementDetector.GetIgnoredElements"/> for a single diagnostic run. It
/// reads an element at most once and must not be kept after the document changes.
/// </remarks>
public interface IIgnoredElements
{
    /// <summary>
    /// Determines whether an element is ignored for the specified diagnostic.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="element">An element of the document this instance was obtained for.</param>
    /// <returns><see langword="true"/> when the element is ignored; otherwise, <see langword="false"/>.</returns>
    bool IsIgnored(string code, Element element);
}