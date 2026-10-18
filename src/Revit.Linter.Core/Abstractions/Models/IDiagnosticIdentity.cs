namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Defines the stable identity and default behavior shared by every diagnostic.</summary>
/// <remarks>
/// Element and document diagnostics have distinct identity types because their registration contracts
/// differ, but every panel, filter and export reads the same members through this interface.
/// </remarks>
public interface IDiagnosticIdentity
{
    /// <summary>Gets the stable diagnostic code.</summary>
    string Code { get; }
    /// <summary>Gets the diagnostic description.</summary>
    string Description { get; }
    /// <summary>Gets the finding message template.</summary>
    string MessageFormat { get; }
    /// <summary>Gets the default severity.</summary>
    DiagnosticSeverity DefaultSeverity { get; }
    /// <summary>Gets whether the diagnostic is enabled by default.</summary>
    bool IsActive { get; }
    /// <summary>Gets whether the diagnostic is obsolete.</summary>
    bool IsObsolete { get; }
    /// <summary>Gets the migration guidance for an obsolete diagnostic.</summary>
    string ObsoleteDescription { get; }
    /// <summary>Gets the trusted system tags the product sets, such as the provenance and applicability tags.</summary>
    IReadOnlyList<string> SystemTags { get; }
    /// <summary>Gets the tags shown to the user: the system tags first, then the user tags.</summary>
    IReadOnlyList<string> Tags { get; }
}
