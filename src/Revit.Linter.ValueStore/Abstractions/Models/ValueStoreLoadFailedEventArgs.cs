namespace Revit.Linter.ValueStore.Abstractions.Models;

public sealed class ValueStoreLoadFailedEventArgs(Type settingsType, string filePath, Exception exception) : EventArgs
{
    public Type SettingsType { get; } = settingsType;
    public string FilePath { get; } = filePath;
    public Exception Exception { get; } = exception;
}
