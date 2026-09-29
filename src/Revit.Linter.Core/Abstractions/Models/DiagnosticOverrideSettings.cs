namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Defines persisted user overrides for one diagnostic.</summary>
public sealed class DiagnosticOverrideSettings
{
    /// <summary>Gets or sets the configured severity.</summary>
    public DiagnosticSeverity Severity { get; set; }
    /// <summary>Gets or sets whether the diagnostic is enabled.</summary>
    public bool IsActive { get; set; }
}
