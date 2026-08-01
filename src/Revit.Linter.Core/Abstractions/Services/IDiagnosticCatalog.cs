using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

public interface IDiagnosticCatalog
{
    IReadOnlyList<ElementDiagnosticRegistration> ElementDiagnostics { get; }
    IReadOnlyList<DocumentDiagnosticRegistration> DocumentDiagnostics { get; }
}
