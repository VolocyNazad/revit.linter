using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementAccentor.Services;

internal sealed class ElementAccentSession(Action restore) : IElementAccentSession
{
    private Action? _restore = restore;

    public void Restore() => Interlocked.Exchange(ref _restore, null)?.Invoke();

    public void Dispose() => Restore();
}
