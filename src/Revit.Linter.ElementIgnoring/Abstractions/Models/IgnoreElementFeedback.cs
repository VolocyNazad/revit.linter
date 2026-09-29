namespace Revit.Linter.ElementIgnoring.Abstractions.Models;

/// <summary>
/// Describes the outcome of a request to ignore an element for a diagnostic.
/// </summary>
/// <param name="Result">The outcome of the request.</param>
/// <param name="Message">An optional message describing a failure.</param>
public sealed record IgnoreElementFeedback(IgnoreElementResult Result, string? Message = null)
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful ignore-element result.</returns>
    public static IgnoreElementFeedback Success() => new(IgnoreElementResult.Success);

    /// <summary>
    /// Creates a failed result with the specified message.
    /// </summary>
    /// <param name="message">The message describing the failure.</param>
    /// <returns>A failed ignore-element result.</returns>
    public static IgnoreElementFeedback Failed(string message) => new(IgnoreElementResult.Failed, message);
}
