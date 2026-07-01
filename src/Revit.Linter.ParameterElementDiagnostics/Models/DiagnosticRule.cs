namespace Revit.Linter.ParameterElementDiagnostics.Models;

internal sealed class DiagnosticRule
{
    public required string Code { get; init; }
    public required string Description { get; init; } = string.Empty;
    public required string Message { get; init; } = string.Empty;
    public DiagnosticSeverity Severity { get; init; } = DiagnosticSeverity.Message;
    public bool IsActive { get; init; } = true;
    public bool IsObsolete { get; init; } = false;
    public string ObsoleteDescription { get; init; } = string.Empty;
    public required string Take { get; init; }
    public required List<ParameterElementData> Parameters { get; init; }
}