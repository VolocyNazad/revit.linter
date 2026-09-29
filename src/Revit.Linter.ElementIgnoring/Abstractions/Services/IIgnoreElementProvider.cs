using Autodesk.Revit.DB;
using Revit.Linter.ElementIgnoring.Abstractions.Models;

namespace Revit.Linter.ElementIgnoring.Abstractions.Services;

/// <summary>
/// Marks elements as ignored for individual diagnostics.
/// </summary>
public interface IIgnoreElementProvider
{
    /// <summary>
    /// Marks an element as ignored for the specified diagnostic.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="element">The element to ignore.</param>
    /// <returns>The outcome of the request.</returns>
    IgnoreElementFeedback Ignore(string code, Element element);
}
