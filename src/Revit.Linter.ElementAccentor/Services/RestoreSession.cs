using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementAccentor.Services;

internal sealed class RestoreSession(Action restore) : IRestoreSession
{
    private readonly object _lock = new();
    private Action? _restore = restore;

    public void Restore()
    {
        lock (_lock)
        {
            Action? restoreAction = _restore;
            if (restoreAction is null) return;

            restoreAction();
            _restore = null;
        }
    }

    public void Dispose() => Restore();
}
