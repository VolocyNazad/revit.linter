using Revit.Linter.ValueStore.Abstractions.Models;

namespace Revit.Linter.ValueStore.Abstractions.Services;

public interface IValueStoreNotificationSource
{
    IDisposable OnLoadFailed(Action<ValueStoreLoadFailedEventArgs> listener);
}
