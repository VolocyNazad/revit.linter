namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Defines the stable identity and default behavior of a document diagnostic.</summary>
/// <param name="code">The stable user-facing diagnostic code.</param>
/// <param name="description">The diagnostic description.</param>
/// <param name="messageFormat">The finding message template.</param>
/// <param name="severity">The severity used when no override exists.</param>
/// <param name="isActive">Whether the diagnostic is enabled by default.</param>
/// <param name="isObsolete">Whether the diagnostic is retained only for compatibility.</param>
/// <param name="obsoleteDescription">Guidance for replacing an obsolete diagnostic.</param>
/// <param name="isExample">Whether the diagnostic came from an installed example configuration.</param>
/// <param name="isTour">Whether the diagnostic belongs to the managed practical-tour configuration.</param>
public sealed class DocumentDiagnosticId(string code, string description, string messageFormat, DiagnosticSeverity severity, bool isActive, bool isObsolete, string obsoleteDescription, bool isExample = false, bool isTour = false)
{
    /// <summary>Gets the stable diagnostic code.</summary>
    public string Code { get; } = code;
    /// <summary>Gets the diagnostic description.</summary>
    public string Description { get; } = description;
    /// <summary>Gets the finding message template.</summary>
    public string MessageFormat { get; } = messageFormat;
    /// <summary>Gets the default severity.</summary>
    public DiagnosticSeverity DefaultSeverity { get; } = severity;
    /// <summary>Gets whether the diagnostic is enabled by default.</summary>
    public bool IsActive { get; } = isActive;
    /// <summary>Gets whether the diagnostic is obsolete.</summary>
    public bool IsObsolete { get; } = isObsolete;
    /// <summary>Gets the migration guidance for an obsolete diagnostic.</summary>
    public string ObsoleteDescription { get; } = obsoleteDescription;
    /// <summary>Gets whether the diagnostic came from an installed example configuration.</summary>
    public bool IsExample { get; } = isExample;
    /// <summary>Gets whether the diagnostic belongs to the managed practical-tour configuration.</summary>
    public bool IsTour { get; } = isTour;
}
