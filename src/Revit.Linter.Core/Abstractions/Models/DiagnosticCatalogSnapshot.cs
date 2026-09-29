namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Provides an immutable view of the diagnostics in one catalog revision.</summary>
public sealed class DiagnosticCatalogSnapshot
{
    /// <summary>Creates a snapshot by copying the supplied registrations.</summary>
    /// <param name="elementDiagnostics">The element diagnostic registrations.</param>
    /// <param name="documentDiagnostics">The document diagnostic registrations.</param>
    public DiagnosticCatalogSnapshot(
        IEnumerable<ElementDiagnosticRegistration> elementDiagnostics,
        IEnumerable<DocumentDiagnosticRegistration> documentDiagnostics)
    {
        ElementDiagnostics = Array.AsReadOnly(elementDiagnostics.ToArray());
        DocumentDiagnostics = Array.AsReadOnly(documentDiagnostics.ToArray());
    }

    /// <summary>Gets the element diagnostic registrations.</summary>
    public IReadOnlyList<ElementDiagnosticRegistration> ElementDiagnostics { get; }
    /// <summary>Gets the document diagnostic registrations.</summary>
    public IReadOnlyList<DocumentDiagnosticRegistration> DocumentDiagnostics { get; }
}
