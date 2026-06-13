namespace Revit.Linter.Core.Abstractions.Models;

public sealed class DocumentDiagnosticIdOverrides(DocumentDiagnosticId id, DiagnosticSeverity severity, bool isActive)
{
    public DocumentDiagnosticId Identity { get; } = id;
    public DiagnosticSeverity Severity { get; set; } = severity;
    public bool IsActive { get; set; } = isActive;
}