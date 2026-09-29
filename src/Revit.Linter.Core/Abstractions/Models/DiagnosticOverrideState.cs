namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Represents a diagnostic's effective severity and activation state.</summary>
/// <param name="Severity">The effective severity.</param>
/// <param name="IsActive">Whether the diagnostic participates in execution.</param>
public readonly record struct DiagnosticOverrideState(DiagnosticSeverity Severity, bool IsActive);
