using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Synchronizes an element diagnostic's effective settings with persistent storage.</summary>
public sealed class ElementDiagnosticIdOverride : DiagnosticIdOverride<ElementDiagnosticId>, IDisposable
{
    private readonly IValueStore<ElementDiagnosticOverridesSettings>? _store;
    private readonly IDisposable? _subscription;

    /// <summary>Creates an override bound to the supplied settings store.</summary>
    /// <param name="id">The diagnostic identity.</param>
    /// <param name="store">The persistent override store.</param>
    public ElementDiagnosticIdOverride(
        ElementDiagnosticId id,
        IValueStore<ElementDiagnosticOverridesSettings> store)
        : base(id, id.DefaultSeverity, id.IsActive)
    {
        _store = store;
        Apply(store.CurrentValue);
        _subscription = store.OnChange(Apply);
    }

    /// <inheritdoc />
    public void Dispose() => _subscription?.Dispose();

    /// <inheritdoc />
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
