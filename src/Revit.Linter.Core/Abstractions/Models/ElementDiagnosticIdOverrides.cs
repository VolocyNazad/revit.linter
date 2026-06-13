namespace Revit.Linter.Core.Abstractions.Models;

public sealed class ElementDiagnosticIdOverrides(ElementDiagnosticId id, DiagnosticSeverity severity, bool isActive)
{
    public ElementDiagnosticId Identity { get; } = id;
    public DiagnosticSeverity Severity { get; set; } = severity;
    public bool IsActive { get; set; } = isActive;
}
