namespace Revit.Linter.Core.Abstractions.Models;

public sealed class DiagnosticCatalogSnapshot
{
    public DiagnosticCatalogSnapshot(
        IEnumerable<ElementDiagnosticRegistration> elementDiagnostics,
        IEnumerable<DocumentDiagnosticRegistration> documentDiagnostics)
    {
        ElementDiagnostics = Array.AsReadOnly(elementDiagnostics.ToArray());
        DocumentDiagnostics = Array.AsReadOnly(documentDiagnostics.ToArray());
    }

    public IReadOnlyList<ElementDiagnosticRegistration> ElementDiagnostics { get; }
    public IReadOnlyList<DocumentDiagnosticRegistration> DocumentDiagnostics { get; }
}
