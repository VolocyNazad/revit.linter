using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.Core.Abstractions.Models;

public sealed class ElementDiagnosticIdOverride : DiagnosticIdOverride<ElementDiagnosticId>, IDisposable
{
    private readonly IValueStore<ElementDiagnosticOverridesSettings>? _store;
    private readonly IDisposable? _subscription;

    public ElementDiagnosticIdOverride(
        ElementDiagnosticId id,
        IValueStore<ElementDiagnosticOverridesSettings> store)
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

    private void Apply(ElementDiagnosticOverridesSettings settings)
    {
        var state = settings.Overrides.TryGetValue(Identity.Code, out var stored)
            ? new DiagnosticOverrideState(stored.Severity, stored.IsActive)
            : new DiagnosticOverrideState(Identity.DefaultSeverity, Identity.IsActive);
        Apply(state);
    }
}