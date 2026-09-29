namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Provides mutable user settings layered over an immutable diagnostic identity.</summary>
/// <typeparam name="TIdentity">The diagnostic identity type.</typeparam>
public abstract class DiagnosticIdOverride<TIdentity>
{
    private readonly object _stateLock = new();
    private DiagnosticOverrideState _state;

    /// <summary>Initializes the effective diagnostic state.</summary>
    /// <param name="identity">The immutable identity.</param>
    /// <param name="severity">The initial severity.</param>
    /// <param name="isActive">The initial activation state.</param>
    protected DiagnosticIdOverride(TIdentity identity, DiagnosticSeverity severity, bool isActive)
    {
        Identity = identity;
        _state = new DiagnosticOverrideState(severity, isActive);
    }

    /// <summary>Gets the immutable diagnostic identity.</summary>
    public TIdentity Identity { get; }

    /// <summary>Gets or persists the effective severity.</summary>
    public DiagnosticSeverity Severity
    {
        get
        {
            lock (_stateLock) return _state.Severity;
        }
        set => Update(CurrentState with { Severity = value });
    }

    /// <summary>Gets or persists whether the diagnostic participates in execution.</summary>
    public bool IsActive
    {
        get
        {
            lock (_stateLock) return _state.IsActive;
        }
        set => Update(CurrentState with { IsActive = value });
    }

    /// <summary>Occurs after persisted settings change the effective state.</summary>
    public event EventHandler<DiagnosticOverrideChangedEventArgs>? Changed;

    /// <summary>Publishes a persisted state without writing it back.</summary>
    /// <param name="state">The observed state.</param>
    protected void Apply(DiagnosticOverrideState state)
    {
        DiagnosticOverrideState previous;
        lock (_stateLock)
        {
            if (_state == state) return;

            previous = _state;
            _state = state;
        }
        Changed?.Invoke(this, new DiagnosticOverrideChangedEventArgs(previous, state));
    }

    /// <summary>Writes a requested state to the owning persistence mechanism.</summary>
    /// <param name="state">The state to persist.</param>
    protected abstract void Persist(DiagnosticOverrideState state);

    private void Update(DiagnosticOverrideState state)
    {
        if (CurrentState == state) return;
        Persist(state);
    }

    private DiagnosticOverrideState CurrentState
    {
        get
        {
            lock (_stateLock) return _state;
        }
    }
}
