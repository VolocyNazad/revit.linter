using Microsoft.Extensions.Logging;

namespace Revit.Linter.Diagnostic.Services;

internal sealed class DiagnosticCatalogSnapshotFactory(
    IEnumerable<IDiagnosticRegistrationProvider> providers,
    IDiagnosticConfigurationErrorState configurationErrorState,
    IBuiltInDiagnosticStateProvider builtInStateProvider,
    IDiagnosticCatalogScope catalogScope,
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
            snapshot = NarrowToScope(snapshot);
            configurationErrorState.CompleteRefresh();
            LogRefreshSummary(snapshot.ElementDiagnostics.Count, snapshot.DocumentDiagnostics.Count);
            return new DiagnosticCatalogSnapshotOwner(snapshot);
        }
        catch
        {
            configurationErrorState.CompleteRefresh();
            // The collected registrations never reached a snapshot, so dispose the components the
            // providers already created instead of leaking them.
            DiagnosticCatalogSnapshotOwner.DisposeComponents(elementDiagnostics, documentDiagnostics);
            throw;
        }
    }

    /// <summary>Publishes only the check a session scoped the catalog to.</summary>
    /// <remarks>
    /// The narrowing runs after validation and after the built-in override report, so an active session
    /// scope never hides configuration problems. The hidden registrations own components that would
    /// otherwise be disposed only when the snapshot retires, so they are disposed here instead.
    /// </remarks>
    private DiagnosticCatalogSnapshot NarrowToScope(DiagnosticCatalogSnapshot snapshot)
    {
        if (catalogScope.FocusedDiagnosticCode is not { } focusedCode) return snapshot;

        bool IsFocused(string code) => string.Equals(code, focusedCode, StringComparison.Ordinal);

        List<ElementDiagnosticRegistration> hiddenElements = snapshot.ElementDiagnostics
            .Where(registration => !IsFocused(registration.Identity.Code))
            .ToList();
        List<DocumentDiagnosticRegistration> hiddenDocuments = snapshot.DocumentDiagnostics
            .Where(registration => !IsFocused(registration.Identity.Code))
            .ToList();
        if (hiddenElements.Count == 0 && hiddenDocuments.Count == 0) return snapshot;

        DiagnosticCatalogSnapshotOwner.DisposeComponents(hiddenElements, hiddenDocuments);
        logger.LogInformation(
            "Diagnostic catalog is narrowed to {DiagnosticCode} for the active session", focusedCode);
        return new DiagnosticCatalogSnapshot(
            snapshot.ElementDiagnostics.Where(registration => IsFocused(registration.Identity.Code)),
            snapshot.DocumentDiagnostics.Where(registration => IsFocused(registration.Identity.Code)));
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
        foreach (ElementDiagnosticRegistration registration in elementDiagnostics.Where(
                     registration => registration.RuleType is null))
        {
            builtInCodes.Add(registration.Identity.Code);
        }

        foreach (DocumentDiagnosticRegistration registration in documentDiagnostics.Where(
                     registration => registration.RuleType is null))
        {
            builtInCodes.Add(registration.Identity.Code);
        }

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

    private void LogRefreshSummary(int elementDiagnosticCount, int documentDiagnosticCount)
    {
        logger.LogInformation(
            "Diagnostic catalog refreshed: {ElementDiagnosticCount} element and {DocumentDiagnosticCount} document diagnostics registered",
            elementDiagnosticCount,
            documentDiagnosticCount);

        // The configuration notification already shows these problems to the user; the log keeps
        // the same details for support reports where the notification was dismissed.
        if (configurationErrorState.CurrentError is { } problems)
            logger.LogWarning(
                "Diagnostic configuration problems: {RuleErrorCount} rule problems. {Problems}",
                configurationErrorState.RuleErrorCount,
                problems);
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
