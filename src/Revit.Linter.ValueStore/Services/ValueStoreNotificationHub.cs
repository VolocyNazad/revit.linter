using Revit.Linter.ValueStore.Abstractions;

namespace Revit.Linter.ValueStore.Services;

internal sealed class ValueStoreNotificationHub : IValueStoreNotificationSource
{
	private readonly object _lock = new();
	private readonly List<Action<ValueStoreLoadFailedEventArgs>> _listeners = [];
	private readonly Dictionary<Type, ValueStoreLoadFailedEventArgs> _failures = [];

	public IDisposable OnLoadFailed(Action<ValueStoreLoadFailedEventArgs> listener)
	{
		if (listener is null) throw new ArgumentNullException(nameof(listener));
		ValueStoreLoadFailedEventArgs[] failures;
		lock (_lock)
		{
			_listeners.Add(listener);
			failures = [.. _failures.Values];
		}
		foreach (ValueStoreLoadFailedEventArgs failure in failures) listener(failure);
		return new Subscription(this, listener);
	}

	public void ReportFailure(Type valueType, string filePath, Exception exception)
	{
		Action<ValueStoreLoadFailedEventArgs>[] listeners;
		var args = new ValueStoreLoadFailedEventArgs(valueType, filePath, exception);
		lock (_lock)
		{
			if (_failures.TryGetValue(valueType, out ValueStoreLoadFailedEventArgs? previous)
				&& previous.Exception.GetType() == exception.GetType()
				&& string.Equals(previous.Exception.Message, exception.Message, StringComparison.Ordinal)) return;
			_failures[valueType] = args;
			listeners = [.. _listeners];
		}
		foreach (Action<ValueStoreLoadFailedEventArgs> listener in listeners) listener(args);
	}

	public void ReportSuccess(Type valueType)
	{
		lock (_lock) _failures.Remove(valueType);
	}

	private void Unsubscribe(Action<ValueStoreLoadFailedEventArgs> listener)
	{
		lock (_lock) _listeners.Remove(listener);
	}

	private sealed class Subscription(ValueStoreNotificationHub owner, Action<ValueStoreLoadFailedEventArgs> listener)
		: IDisposable
	{
		private ValueStoreNotificationHub? _owner = owner;
		public void Dispose() => Interlocked.Exchange(ref _owner, null)?.Unsubscribe(listener);
	}
}