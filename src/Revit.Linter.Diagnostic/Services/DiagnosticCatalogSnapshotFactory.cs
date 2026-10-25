using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.Diagnostic.Services;

internal sealed class DiagnosticCatalogSnapshotFactory(
    IEnumerable<IDiagnosticRegistrationProvider> providers,
    IDiagnosticConfigurationErrorState configurationErrorState,
    IBuiltInDiagnosticStateProvider builtInStateProvider,
    ILogger<DiagnosticCatalogSnapshotFactory> logger) : IDiagnosticCatalogSnapshotFactory
{
    private readonly IDiagnosticRegistrationProvider[] _providers = providers.ToArray();

    public DiagnosticCatalogSnapshotOwner Create()
    {
        configurationErrorState.BeginRefresh();
        List<ElementDiagnosticRegistration> elementDiagnostics = [];
        List<DocumentDiagnosticRegistration> documentDiagnostics = [];
        try
        {
            foreach (IDiagnosticRegistrationProvider provider in _providers)
            {
                elementDiagnostics.AddRange(provider.GetElementDiagnostics());
                documentDiagnostics.AddRange(provider.GetDocumentDiagnostics());
            }

            elementDiagnostics = KeepFirstRegistration(elementDiagnostics, registration => registration.Identity.Code);
            documentDiagnostics = KeepFirstRegistration(documentDiagnostics, registration => registration.Identity.Code);
            ReportUnknownBuiltInOverrides(elementDiagnostics, documentDiagnostics);

            var snapshot = new DiagnosticCatalogSnapshot(elementDiagnostics, documentDiagnostics);
            Validate(snapshot);
            configurationErrorState.CompleteRefresh();
            return new DiagnosticCatalogSnapshotOwner(snapshot);
        }
        catch
        {
            configurationErrorState.CompleteRefresh();
            throw;
        }
    }

    private List<T> KeepFirstRegistration<T>(List<T> registrations, Func<T, string> getCode)
    {
        HashSet<string> codes = new(StringComparer.Ordinal);
        List<T> result = new(registrations.Count);
        foreach (T registration in registrations)
        {
            string code = getCode(registration);
            if (!codes.Add(code))
            {
                logger.LogDebug(
                    "Diagnostic {DiagnosticCode} was registered more than once; keeping the first registration",
                    code);
                continue;
            }

            result.Add(registration);
        }

        return result;
    }

    private void ReportUnknownBuiltInOverrides(
        IReadOnlyList<ElementDiagnosticRegistration> elementDiagnostics,
        IReadOnlyList<DocumentDiagnosticRegistration> documentDiagnostics)
    {
        HashSet<string> builtInCodes = new(StringComparer.Ordinal);
        foreach (ElementDiagnosticRegistration registration in elementDiagnostics)
            if (registration.RuleType is null) builtInCodes.Add(registration.Identity.Code);
        foreach (DocumentDiagnosticRegistration registration in documentDiagnostics)
            if (registration.RuleType is null) builtInCodes.Add(registration.Identity.Code);

        List<string> errors = [];
        foreach (string code in builtInStateProvider.OverriddenCodes)
        {
            if (builtInCodes.Contains(code)) continue;
            errors.Add(
                $"diagnostics.config.yaml: built-in override '{code}' was skipped: no built-in diagnostic has this code");
        }

        if (errors.Count > 0)
            configurationErrorState.AddRuleErrors(errors);
    }

    private static void Validate(DiagnosticCatalogSnapshot snapshot)
    {
        foreach (ElementDiagnosticRegistration registration in snapshot.ElementDiagnostics)
        {
            ValidateCode(registration.Identity.Code, registration.Diagnostic.Identity.Code, "diagnostic");
            ValidateCode(registration.Identity.Code, registration.Filter.Identity.Code, "element filter");
            ValidateCode(registration.Identity.Code, registration.DocumentFilter.Identity.Code, "document filter");
            foreach (IElementFix fix in registration.Fixes)
                ValidateCode(registration.Identity.Code, fix.Identity.Code, "fix");
            foreach (IElementVisualizationPipeline pipeline in registration.VisualizationPipelines)
                ValidateCode(registration.Identity.Code, pipeline.Identity.Code, "visualization pipeline");
        }

        foreach (DocumentDiagnosticRegistration registration in snapshot.DocumentDiagnostics)
        {
            ValidateCode(registration.Identity.Code, registration.Diagnostic.Identity.Code, "diagnostic");
            ValidateCode(registration.Identity.Code, registration.Filter.Identity.Code, "document filter");
            foreach (IDocumentFix fix in registration.Fixes)
                ValidateCode(registration.Identity.Code, fix.Identity.Code, "fix");
        }
    }

    private static void ValidateCode(string registrationCode, string componentCode, string componentName)
    {
        if (!string.Equals(registrationCode, componentCode, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Diagnostic registration '{registrationCode}' contains {componentName} for '{componentCode}'.");
    }
}
