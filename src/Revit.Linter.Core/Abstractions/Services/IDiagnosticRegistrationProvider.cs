using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Creates independently owned diagnostic registrations for catalog revisions.</summary>
public interface IDiagnosticRegistrationProvider
{
    /// <summary>
    /// Creates a new independently owned set of element diagnostic registrations.
    /// The returned components become owned by the catalog snapshot.
    /// </summary>
    /// <returns>New element diagnostic registrations and their owned runtime components.</returns>
    IEnumerable<ElementDiagnosticRegistration> GetElementDiagnostics();

    /// <summary>
    /// Creates a new independently owned set of document diagnostic registrations.
    /// The returned components become owned by the catalog snapshot.
    /// </summary>
    /// <returns>New document diagnostic registrations and their owned runtime components.</returns>
    IEnumerable<DocumentDiagnosticRegistration> GetDocumentDiagnostics();
}
