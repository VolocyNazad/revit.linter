namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Builds the result list an element diagnostic returns.</summary>
/// <remarks>
/// Most checks report one result per element, so the helpers keep the common path allocation-free:
/// a valid result returns the shared one-element list, and only a finding allocates.
/// </remarks>
public static class DiagnosticFeedbacks
{
    /// <summary>Gets the one-element list that reports a clean element, shared by every check.</summary>
    public static IReadOnlyList<DiagnosticFeedback> Valid { get; } = [DiagnosticFeedback.Valid];

    /// <summary>Wraps one result into the list an element diagnostic returns.</summary>
    /// <param name="feedback">The result of checking one element.</param>
    /// <returns>The shared valid list when the result is valid; otherwise a one-element list.</returns>
    public static IReadOnlyList<DiagnosticFeedback> Of(DiagnosticFeedback feedback) =>
        feedback.Verdict == DiagnosticVerdict.Valid ? Valid : [feedback];
}
