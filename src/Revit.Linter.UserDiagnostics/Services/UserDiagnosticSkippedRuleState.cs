using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.UserDiagnostics.Services;

/// <summary>Holds the description of the user rules skipped by the last configuration load.</summary>
/// <remarks>
/// Covers rules rejected while composing their fix or visualization pipelines; file parsing
/// failures stay with <see cref="UserDiagnosticConfigurationErrorState"/>.
/// </remarks>
internal sealed class UserDiagnosticSkippedRuleState : IDiagnosticConfigurationErrorSource
{
    private readonly object _sync = new();
    private string? _currentError;

    /// <inheritdoc />
    public string? CurrentError
    {
        get
        {
            lock (_sync)
                return _currentError;
        }
    }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <summary>Replaces the current description.</summary>
    /// <param name="error">The description, or <see langword="null"/> when every rule was accepted.</param>
    /// <returns><see langword="true"/> when the description changed and <see cref="Changed"/> was raised.</returns>
    public bool Set(string? error)
    {
        bool changed;
        lock (_sync)
        {
            changed = !string.Equals(_currentError, error, StringComparison.Ordinal);
            _currentError = error;
        }

        if (changed) Changed?.Invoke(this, EventArgs.Empty);
        return changed;
    }
}
