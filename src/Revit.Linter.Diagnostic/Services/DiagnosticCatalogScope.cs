using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.Diagnostic.Services;

/// <summary>Narrows the published diagnostics to one check for a guided session.</summary>
/// <remarks>
/// The scope is session state, not configuration: it never writes to the configuration files, so ending
/// the session restores the full catalog without anything to undo. The catalog subscribes to
/// <see cref="Changed"/> and republishes its snapshot, which is why the panes, runs and reports all see
/// the narrowed set.
/// </remarks>
internal sealed class DiagnosticCatalogScope : IDiagnosticCatalogScope
{
    private readonly object _sync = new();
    private string? _focusedDiagnosticCode;

    /// <inheritdoc />
    public event EventHandler? Changed;

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
    public void Focus(string diagnosticCode)
    {
        if (string.IsNullOrWhiteSpace(diagnosticCode))
            throw new ArgumentException("The diagnostic code is required.", nameof(diagnosticCode));

        SetFocusedDiagnosticCode(diagnosticCode);
    }

    /// <inheritdoc />
    public void Clear() => SetFocusedDiagnosticCode(null);

    private void SetFocusedDiagnosticCode(string? diagnosticCode)
    {
        EventHandler? changed;
        lock (_sync)
        {
            if (string.Equals(_focusedDiagnosticCode, diagnosticCode, StringComparison.Ordinal)) return;
            _focusedDiagnosticCode = diagnosticCode;
            changed = Changed;
        }

        changed?.Invoke(this, EventArgs.Empty);
    }
}
