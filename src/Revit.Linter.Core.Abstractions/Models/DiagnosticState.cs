namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>
/// Holds the effective, user-editable state of one registered diagnostic.
/// </summary>
/// <remarks>
/// The state is created for one catalog snapshot. The diagnostic list mutates it after persisting the
/// same change to the configuration file, and the diagnostic service reads it during a run. A later
/// configuration reload replaces the whole snapshot, so the state is never the source of truth on disk.
/// </remarks>
public sealed class DiagnosticState(bool isActive, DiagnosticSeverity severity, bool isHidden)
{
    /// <summary>Gets or sets whether the diagnostic participates in execution.</summary>
    public bool IsActive { get; set; } = isActive;

    /// <summary>Gets or sets the severity of the findings the diagnostic reports.</summary>
    public DiagnosticSeverity Severity { get; set; } = severity;

    /// <summary>Gets or sets whether the diagnostic is hidden from the interface, execution and reports.</summary>
    public bool IsHidden { get; set; } = isHidden;

    /// <summary>Gets whether the diagnostic currently participates in execution.</summary>
    public bool IsEnabled => IsActive && !IsHidden;
}
