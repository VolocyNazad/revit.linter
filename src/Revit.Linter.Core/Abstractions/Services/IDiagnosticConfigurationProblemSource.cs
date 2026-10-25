using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Exposes the structured problems found in the diagnostic configuration files.</summary>
/// <remarks>
/// This source complements <see cref="IDiagnosticConfigurationErrorSource"/>: the host localizes these
/// problems, while the plain description remains available for logs and callers that only show text.
/// </remarks>
public interface IDiagnosticConfigurationProblemSource
{
    /// <summary>Gets the file problems of the last load, or an empty list when every file was readable.</summary>
    IReadOnlyList<DiagnosticConfigurationFileProblem> CurrentProblems { get; }

    /// <summary>
    /// Gets the descriptions of the skipped rules, or <see langword="null"/> when no rule was skipped.
    /// </summary>
    /// <remarks>
    /// This is the rule-only part of <see cref="IDiagnosticConfigurationErrorSource.CurrentError"/>; the
    /// file problems are exposed separately so the host does not show the same file twice.
    /// </remarks>
    string? CurrentRuleError { get; }

    /// <summary>Occurs when <see cref="CurrentProblems"/> or <see cref="CurrentRuleError"/> changes.</summary>
    event EventHandler? Changed;
}
