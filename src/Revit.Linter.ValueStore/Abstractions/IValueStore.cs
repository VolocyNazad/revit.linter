namespace Revit.Linter.ValueStore.Abstractions;

public interface IValueStore<out T> where T : class
{
	T CurrentValue { get; }
	IDisposable OnChange(Action<T> listener);
	void Update(Action<T> change);
}