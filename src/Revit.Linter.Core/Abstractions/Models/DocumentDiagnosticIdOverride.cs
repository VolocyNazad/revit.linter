using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.Core.Abstractions.Models;

public sealed class DocumentDiagnosticIdOverride : DiagnosticIdOverride<DocumentDiagnosticId>, IDisposable
{
    private readonly IValueStore<DocumentDiagnosticOverridesSettings>? _store;
    private readonly IDisposable? _subscription;

    public DocumentDiagnosticIdOverride(
        DocumentDiagnosticId id,
        IValueStore<DocumentDiagnosticOverridesSettings> store)
        : base(id, id.DefaultSeverity, id.IsActive)
    {
        _store = store;
        Apply(store.CurrentValue);
        _subscription = store.OnChange(Apply);
    }

    public void Dispose() => _subscription?.Dispose();

    protected override void Persist(DiagnosticOverrideState state)
    {
        _store?.Update(settings => settings.Overrides[Identity.Code] = new DiagnosticOverrideSettings
        {
            Severity = state.Severity,
            IsActive = state.IsActive,
        });
    }

    private void Apply(DocumentDiagnosticOverridesSettings settings)
    {
        var state = settings.Overrides.TryGetValue(Identity.Code, out var stored)
            ? new DiagnosticOverrideState(stored.Severity, stored.IsActive)
            : new DiagnosticOverrideState(Identity.DefaultSeverity, Identity.IsActive);
        Apply(state);
    }
}