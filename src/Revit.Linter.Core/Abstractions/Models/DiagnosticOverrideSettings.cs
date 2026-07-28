namespace Revit.Linter.Core.Abstractions.Models;

public sealed class DiagnosticOverrideSettings
{
    public DiagnosticSeverity Severity { get; set; }
    public bool IsActive { get; set; }
}
