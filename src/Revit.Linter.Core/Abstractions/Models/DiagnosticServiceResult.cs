namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Indicates whether a diagnostic operation completed successfully.</summary>
public enum DiagnosticServiceResult
{
    /// <summary>The operation completed and published its reports.</summary>
    Success,
    /// <summary>The operation did not complete successfully.</summary>
    Failed
}
