namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>
/// Exposes the configuration rules a diagnostic module had to skip while loading its configuration file.
/// </summary>
/// <remarks>
/// A module keeps loading the valid rules of its file and reports the skipped ones here instead of throwing,
/// so one faulty rule neither removes the others nor surfaces later as a failure during a diagnostic run.
/// </remarks>
public interface IDiagnosticConfigurationErrorSource
{
    /// <summary>
    /// Gets the localized, user-facing description of the skipped rules, or <see langword="null"/> when the
    /// last load accepted every rule.
    /// </summary>
    string? CurrentError { get; }

    /// <summary>Occurs when <see cref="CurrentError"/> changes.</summary>
    event EventHandler? Changed;
}
