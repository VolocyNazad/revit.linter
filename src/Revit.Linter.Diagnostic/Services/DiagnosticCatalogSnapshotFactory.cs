using Revit.Linter.Diagnostic.Infrastructure.Exceptions;

namespace Revit.Linter.Diagnostic.Services;

internal sealed class DiagnosticCatalogSnapshotFactory(
    IEnumerable<IDiagnosticRegistrationProvider> providers) : IDiagnosticCatalogSnapshotFactory
{
    private readonly IDiagnosticRegistrationProvider[] _providers = providers.ToArray();

    public DiagnosticCatalogSnapshotOwner Create()
    {
        List<ElementDiagnosticRegistration> elementDiagnostics = [];
        List<DocumentDiagnosticRegistration> documentDiagnostics = [];
        DiagnosticCatalogSnapshot snapshot;
        try
        {
            foreach (IDiagnosticRegistrationProvider provider in _providers)
            {
                elementDiagnostics.AddRange(provider.GetElementDiagnostics());
                documentDiagnostics.AddRange(provider.GetDocumentDiagnostics());
            }
            snapshot = new DiagnosticCatalogSnapshot(elementDiagnostics, documentDiagnostics);
        }
        catch
        {
            new DiagnosticCatalogSnapshotOwner(
                new DiagnosticCatalogSnapshot(elementDiagnostics, documentDiagnostics)).Dispose();
            throw;
        }

        var owner = new DiagnosticCatalogSnapshotOwner(snapshot);
        try
        {
            Validate(snapshot);
            return owner;
        }
        catch
        {
            owner.Dispose();
            throw;
        }
    }

    private static void Validate(DiagnosticCatalogSnapshot snapshot)
    {
        ValidateUniqueCodes(snapshot.ElementDiagnostics.Select(registration => registration.Identity.Code));
        ValidateUniqueCodes(snapshot.DocumentDiagnostics.Select(registration => registration.Identity.Code));

        foreach (ElementDiagnosticRegistration registration in snapshot.ElementDiagnostics)
        {
            ValidateCode(registration.Identity.Code, registration.Diagnostic.Identity.Code, "diagnostic");
            ValidateCode(registration.Identity.Code, registration.Filter.Identity.Code, "element filter");
            ValidateCode(registration.Identity.Code, registration.DocumentFilter.Identity.Code, "document filter");
            foreach (IElementFix fix in registration.Fixes)
                ValidateCode(registration.Identity.Code, fix.Identity.Code, "fix");
        }

        foreach (DocumentDiagnosticRegistration registration in snapshot.DocumentDiagnostics)
        {
            ValidateCode(registration.Identity.Code, registration.Diagnostic.Identity.Code, "diagnostic");
            ValidateCode(registration.Identity.Code, registration.Filter.Identity.Code, "document filter");
            foreach (IDocumentFix fix in registration.Fixes)
                ValidateCode(registration.Identity.Code, fix.Identity.Code, "fix");
        }
    }

    private static void ValidateUniqueCodes(IEnumerable<string> codes)
    {
        string? duplicateCode = codes
            .GroupBy(code => code, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateCode is not null)
            throw new DuplicateDiagnosticIdException(duplicateCode);
    }

    private static void ValidateCode(string registrationCode, string componentCode, string componentName)
    {
        if (!string.Equals(registrationCode, componentCode, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Diagnostic registration '{registrationCode}' contains {componentName} for '{componentCode}'.");
    }
}
