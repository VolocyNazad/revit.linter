namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Identifies the source of a diagnostic catalog change.</summary>
public enum DiagnosticCatalogChangeOrigin
{
    /// <summary>The catalog was explicitly refreshed by the application.</summary>
    Manual,
    /// <summary>The catalog was refreshed after its external configuration changed.</summary>
    ExternalFile,
    /// <summary>The catalog was refreshed after a session scope narrowed or restored its diagnostics.</summary>
    Scope,
}
