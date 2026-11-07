using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.Diagnostic.Services;

/// <summary>Narrows the published diagnostics to one check for a guided session.</summary>
/// <remarks>
/// The scope is session state, not configuration: it never writes to the configuration files, so ending
/// the session restores the full catalog without anything to undo. The catalog subscribes to
/// <see cref="Changed"/> and republishes its snapshot, which is why the panes, runs and reports all see
/// the session's set of checks.
/// </remarks>
internal sealed class DiagnosticCatalogScope : IDiagnosticCatalogScope
{
    private readonly object _sync = new();
    private bool _isSessionActive;
    private string? _focusedDiagnosticCode;

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public bool IsSessionActive
    {
        get
        {
            lock (_sync)
                return _isSessionActive;
        }
    }

    /// <inheritdoc />
    public string? FocusedDiagnosticCode
    {
        get
        {
            lock (_sync)
                return _focusedDiagnosticCode;
        }
    }

    /// <inheritdoc />
    public void BeginSession() => SetState(isSessionActive: true, focusedDiagnosticCode: null);

    /// <inheritdoc />
    public void EndSession() => SetState(isSessionActive: false, focusedDiagnosticCode: null);

    /// <inheritdoc />
    public void Focus(string diagnosticCode)
    {
        if (string.IsNullOrWhiteSpace(diagnosticCode))
            throw new ArgumentException("The diagnostic code is required.", nameof(diagnosticCode));

        SetState(isSessionActive: true, diagnosticCode);
    }

    /// <inheritdoc />
    public void Clear() => SetState(_isSessionActive, focusedDiagnosticCode: null);

    private void SetState(bool isSessionActive, string? focusedDiagnosticCode)
    {
        EventHandler? changed;
        lock (_sync)
        {
            if (_isSessionActive == isSessionActive
                && string.Equals(_focusedDiagnosticCode, focusedDiagnosticCode, StringComparison.Ordinal))
                return;

            _isSessionActive = isSessionActive;
            _focusedDiagnosticCode = focusedDiagnosticCode;
            changed = Changed;
        }

        changed?.Invoke(this, EventArgs.Empty);
    }
}
