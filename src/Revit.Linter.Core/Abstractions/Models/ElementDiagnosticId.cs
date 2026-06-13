namespace Revit.Linter.Core.Abstractions.Models;

public sealed class ElementDiagnosticId(string code, string description, string messageFormat, DiagnosticSeverity defaultSeverity, bool isActive, bool isObsolete, string obsoleteDescription)
{
    public string Code { get; } = code;
    public string Description { get; } = description;
    public string MessageFormat { get; } = messageFormat;
    public DiagnosticSeverity DefaultSeverity { get; } = defaultSeverity;
    public bool IsActive { get; } = isActive;
    public bool IsObsolete { get; } = isObsolete;
    public string ObsoleteDescription { get; } = obsoleteDescription;
}
