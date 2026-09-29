namespace Revit.Linter.ElementIgnoring.Abstractions.Models;

/// <summary>
/// Identifies the outcome of a request to ignore an element.
/// </summary>
public enum IgnoreElementResult
{
    /// <summary>
    /// The element could not be marked as ignored.
    /// </summary>
    Failed,

    /// <summary>
    /// The element was marked as ignored.
    /// </summary>
    Success
}
