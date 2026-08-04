namespace Revit.Linter.Diagnostic.Services;

internal sealed class DebouncedAction(TimeSpan delay, Action action) : IDisposable
{
    private readonly TimeSpan _delay = delay;
    private readonly Timer _timer = new(_ => action(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

    public void Schedule() => _timer.Change(_delay, Timeout.InfiniteTimeSpan);

    public void Dispose() => _timer.Dispose();
}
