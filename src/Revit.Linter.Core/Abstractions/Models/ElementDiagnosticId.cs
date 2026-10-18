namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Defines the stable identity and default behavior of an element diagnostic.</summary>
/// <param name="code">The stable user-facing diagnostic code.</param>
/// <param name="description">The diagnostic description.</param>
/// <param name="messageFormat">The finding message template.</param>
/// <param name="defaultSeverity">The severity used when no override exists.</param>
/// <param name="isActive">Whether the diagnostic is enabled by default.</param>
/// <param name="isObsolete">Whether the diagnostic is retained only for compatibility.</param>
/// <param name="obsoleteDescription">Guidance for replacing an obsolete diagnostic.</param>
/// <param name="systemTags">The trusted system tags the product sets; <see langword="null"/> when it has none.</param>
/// <param name="tags">The user tags used to find the diagnostic; <see langword="null"/> when it has none.</param>
public sealed class ElementDiagnosticId(string code, string description, string messageFormat, DiagnosticSeverity defaultSeverity, bool isActive, bool isObsolete, string obsoleteDescription, IEnumerable<string>? systemTags = null, IEnumerable<string>? tags = null) : IDiagnosticIdentity
{
    /// <summary>Gets the stable diagnostic code.</summary>
    public string Code { get; } = code;
    /// <summary>Gets the diagnostic description.</summary>
    public string Description { get; } = description;
    /// <summary>Gets the finding message template.</summary>
    public string MessageFormat { get; } = messageFormat;
    /// <summary>Gets the default severity.</summary>
    public DiagnosticSeverity DefaultSeverity { get; } = defaultSeverity;
    /// <summary>Gets whether the diagnostic is enabled by default.</summary>
    public bool IsActive { get; } = isActive;
    /// <summary>Gets whether the diagnostic is obsolete.</summary>
    public bool IsObsolete { get; } = isObsolete;
    /// <summary>Gets the migration guidance for an obsolete diagnostic.</summary>
    public string ObsoleteDescription { get; } = obsoleteDescription;
    /// <summary>Gets the trusted system tags the product sets.</summary>
    public IReadOnlyList<string> SystemTags { get; } = DiagnosticTags.Normalize(systemTags);
    /// <summary>Gets the tags shown to the user: the system tags first, then the user tags.</summary>
    public IReadOnlyList<string> Tags { get; } = DiagnosticTags.Merge(systemTags, tags);
}
