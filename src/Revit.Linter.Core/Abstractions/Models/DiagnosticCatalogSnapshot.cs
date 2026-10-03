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

    /// <summary>
    /// Finds the documentation page of the diagnostic registered under the specified code.
    /// </summary>
    /// <param name="code">The diagnostic code, compared ordinally.</param>
    /// <returns>
    /// The page of the first matching element diagnostic, then of the first matching document diagnostic;
    /// <see langword="null"/> when the code is not registered or its module provides no page.
    /// </returns>
    public DocumentationPage? FindDocumentation(string code) =>
        ElementDiagnostics.FirstOrDefault(registration =>
            string.Equals(registration.Identity.Code, code, StringComparison.Ordinal))?.Documentation
        ?? DocumentDiagnostics.FirstOrDefault(registration =>
            string.Equals(registration.Identity.Code, code, StringComparison.Ordinal))?.Documentation;

    /// <summary>Gets the element diagnostic registrations.</summary>
    public IReadOnlyList<ElementDiagnosticRegistration> ElementDiagnostics { get; }
    /// <summary>Gets the document diagnostic registrations.</summary>
    public IReadOnlyList<DocumentDiagnosticRegistration> DocumentDiagnostics { get; }
}
