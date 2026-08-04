using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

public interface IDiagnosticRegistrationProvider
{
    /// <summary>
    /// Creates a new independently owned set of element diagnostic registrations.
    /// The returned components become owned by the catalog snapshot.
    /// </summary>
    IEnumerable<ElementDiagnosticRegistration> GetElementDiagnostics();

    /// <summary>
    /// Creates a new independently owned set of document diagnostic registrations.
    /// The returned components become owned by the catalog snapshot.
    /// </summary>
    IEnumerable<DocumentDiagnosticRegistration> GetDocumentDiagnostics();
}
