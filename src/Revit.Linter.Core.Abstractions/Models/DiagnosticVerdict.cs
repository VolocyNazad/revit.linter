namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Defines whether a checked target satisfies a diagnostic.</summary>
public enum DiagnosticVerdict
{
    /// <summary>The target satisfies the diagnostic.</summary>
    Valid,
    /// <summary>The target produces a diagnostic finding.</summary>
    NotValid
}
