namespace Revit.Linter.ValueStore.Abstractions;

public interface IValueStoreNotificationSource
{
	IDisposable OnLoadFailed(Action<ValueStoreLoadFailedEventArgs> listener);
}