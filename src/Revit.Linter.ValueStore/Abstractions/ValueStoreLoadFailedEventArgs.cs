namespace Revit.Linter.ValueStore.Abstractions;

public sealed class ValueStoreLoadFailedEventArgs(Type valueType, string filePath, Exception exception) : EventArgs
{
	public Type ValueType { get; } = valueType;
	public string FilePath { get; } = filePath;
	public Exception Exception { get; } = exception;
}