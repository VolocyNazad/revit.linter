using Revit.Linter.UserDiagnostics.Abstractions.Services;

namespace Revit.Linter.UserDiagnostics.Services;

internal sealed class UserDiagnosticConfigurationErrorState : IUserDiagnosticConfigurationErrorSource
{
    private readonly object _sync = new();
    private Exception? _currentError;

    public Exception? CurrentError
    {
        get
        {
            lock (_sync)
                return _currentError;
        }
    }

    public event EventHandler? Changed;

    public bool Set(Exception error)
    {
        bool changed;
        lock (_sync)
        {
            changed = _currentError?.GetType() != error.GetType() ||
                      !string.Equals(_currentError?.Message, error.Message, StringComparison.Ordinal);
            _currentError = error;
        }

        if (changed) Changed?.Invoke(this, EventArgs.Empty);
        return changed;
    }

    public void Clear()
    {
        bool changed;
        lock (_sync)
        {
            changed = _currentError is not null;
            _currentError = null;
        }

        if (changed) Changed?.Invoke(this, EventArgs.Empty);
    }
}
