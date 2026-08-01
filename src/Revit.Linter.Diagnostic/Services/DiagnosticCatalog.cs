using Revit.Linter.Diagnostic.Infrastructure.Exceptions;
using System.Runtime.CompilerServices;

namespace Revit.Linter.Diagnostic.Services;

internal sealed class DiagnosticCatalog : IDiagnosticCatalog, IDisposable
{
    public DiagnosticCatalog(IEnumerable<IDiagnosticRegistrationProvider> providers)
    {
        IDiagnosticRegistrationProvider[] providerArray = providers.ToArray();
        ElementDiagnostics = providerArray.SelectMany(provider => provider.GetElementDiagnostics()).ToArray();
        DocumentDiagnostics = providerArray.SelectMany(provider => provider.GetDocumentDiagnostics()).ToArray();
        try
        {
            Validate();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public IReadOnlyList<ElementDiagnosticRegistration> ElementDiagnostics { get; }
    public IReadOnlyList<DocumentDiagnosticRegistration> DocumentDiagnostics { get; }

    public void Dispose()
    {
        var disposed = new HashSet<object>(ReferenceComparer.Instance);
        foreach (object component in GetOwnedComponents())
        {
            if (component is IDisposable disposable && disposed.Add(component))
                disposable.Dispose();
        }
    }

    private void Validate()
    {
        ValidateUniqueCodes(ElementDiagnostics.Select(registration => registration.Identity.Code));
        ValidateUniqueCodes(DocumentDiagnostics.Select(registration => registration.Identity.Code));

        foreach (ElementDiagnosticRegistration registration in ElementDiagnostics)
        {
            ValidateCode(registration.Identity.Code, registration.Diagnostic.Identity.Code, "diagnostic");
            ValidateCode(registration.Identity.Code, registration.Filter.Identity.Code, "element filter");
            ValidateCode(registration.Identity.Code, registration.DocumentFilter.Identity.Code, "document filter");
            foreach (IElementFix fix in registration.Fixes)
                ValidateCode(registration.Identity.Code, fix.Identity.Code, "fix");
        }

        foreach (DocumentDiagnosticRegistration registration in DocumentDiagnostics)
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

    private IEnumerable<object> GetOwnedComponents()
    {
        foreach (ElementDiagnosticRegistration registration in ElementDiagnostics)
        {
            yield return registration.Diagnostic;
            yield return registration.Filter;
            yield return registration.DocumentFilter;
            yield return registration.Override;
            foreach (IElementFix fix in registration.Fixes) yield return fix;
        }

        foreach (DocumentDiagnosticRegistration registration in DocumentDiagnostics)
        {
            yield return registration.Diagnostic;
            yield return registration.Filter;
            yield return registration.Override;
            foreach (IDocumentFix fix in registration.Fixes) yield return fix;
        }
    }

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
        public static ReferenceComparer Instance { get; } = new();
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
