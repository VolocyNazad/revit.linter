namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Defines the user-facing importance of a diagnostic finding.</summary>
public enum DiagnosticSeverity
{
    /// <summary>Provides informational feedback.</summary>
    Message,
    /// <summary>Identifies a condition that should be reviewed.</summary>
    Warning,
    /// <summary>Identifies a condition that requires corrective action.</summary>
    Error,
}
