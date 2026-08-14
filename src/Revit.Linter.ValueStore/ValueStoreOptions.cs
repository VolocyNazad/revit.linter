namespace Revit.Linter.ValueStore;

public sealed class ValueStoreOptions
{
	public string DirectoryPath { get; set; } = string.Empty;
	public Func<Type, string>? FileNameFactory { get; set; }
	public bool WatchForExternalChanges { get; set; }
	public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);
}