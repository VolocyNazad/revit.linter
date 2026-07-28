namespace Revit.Linter.Core.Abstractions.Models;

public readonly record struct DiagnosticOverrideState(DiagnosticSeverity Severity, bool IsActive);
