using Revit.Linter.ValueStore.Abstractions.Models;
using Revit.Linter.ValueStore.Abstractions.Services;

namespace Revit.Linter.ValueStore.Services;

internal sealed class ValueStoreNotificationHub : IValueStoreNotificationSource
{
    private readonly object _sync = new();
    private readonly List<Action<ValueStoreLoadFailedEventArgs>> _listeners = [];
    private readonly Dictionary<Type, ValueStoreLoadFailedEventArgs> _failures = [];

    public IDisposable OnLoadFailed(Action<ValueStoreLoadFailedEventArgs> listener)
    {
        ValueStoreLoadFailedEventArgs[] failures;
        lock (_sync)
        {
            _listeners.Add(listener);
            failures = [.. _failures.Values];
        }
        foreach (ValueStoreLoadFailedEventArgs failure in failures) listener(failure);
        return new Subscription(this, listener);
    }

    public void ReportFailure(Type settingsType, string filePath, Exception exception)
    {
        Action<ValueStoreLoadFailedEventArgs>[] listeners;
        var args = new ValueStoreLoadFailedEventArgs(settingsType, filePath, exception);
        lock (_sync)
        {
            if (_failures.TryGetValue(settingsType, out ValueStoreLoadFailedEventArgs? previous) &&
                previous.Exception.GetType() == exception.GetType() &&
                string.Equals(previous.Exception.Message, exception.Message, StringComparison.Ordinal)) return;
            _failures[settingsType] = args;
            listeners = [.. _listeners];
        }
        foreach (Action<ValueStoreLoadFailedEventArgs> listener in listeners) listener(args);
    }

    public void ReportSuccess(Type settingsType)
    {
        lock (_sync) _failures.Remove(settingsType);
    }

    private void Unsubscribe(Action<ValueStoreLoadFailedEventArgs> listener)
    {
        lock (_sync) _listeners.Remove(listener);
    }

    private sealed class Subscription(ValueStoreNotificationHub owner, Action<ValueStoreLoadFailedEventArgs> listener)
        : IDisposable
    {
        private ValueStoreNotificationHub? _owner = owner;
        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.Unsubscribe(listener);
    }
}
