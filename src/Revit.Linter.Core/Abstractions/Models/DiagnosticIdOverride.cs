namespace Revit.Linter.Core.Abstractions.Models;

public abstract class DiagnosticIdOverride<TIdentity>
{
    private readonly object _stateLock = new();
    private DiagnosticOverrideState _state;

    protected DiagnosticIdOverride(TIdentity identity, DiagnosticSeverity severity, bool isActive)
    {
        Identity = identity;
        _state = new DiagnosticOverrideState(severity, isActive);
    }

    public TIdentity Identity { get; }

    public DiagnosticSeverity Severity
    {
        get
        {
            lock (_stateLock) return _state.Severity;
        }
        set => Update(CurrentState with { Severity = value });
    }

    public bool IsActive
    {
        get
        {
            lock (_stateLock) return _state.IsActive;
        }
        set => Update(CurrentState with { IsActive = value });
    }

    public event EventHandler<DiagnosticOverrideChangedEventArgs>? Changed;

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
