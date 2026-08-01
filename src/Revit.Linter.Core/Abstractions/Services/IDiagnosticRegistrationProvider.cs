using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

public interface IDiagnosticRegistrationProvider
{
    IEnumerable<ElementDiagnosticRegistration> GetElementDiagnostics() => [];
    IEnumerable<DocumentDiagnosticRegistration> GetDocumentDiagnostics() => [];
}
