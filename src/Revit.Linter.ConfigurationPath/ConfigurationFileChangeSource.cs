namespace Revit.Linter.ConfigurationPath;

public sealed class ConfigurationFileChangeSource : IDisposable
{
    private readonly object _sync = new();
    private readonly List<Action> _listeners = [];
    private readonly FileSystemWatcher _watcher;
    private bool _disposed;

    public ConfigurationFileChangeSource(string filePath)
    {
        string directory = Path.GetDirectoryName(filePath)
            ?? throw new ArgumentException("Configuration file path must contain a directory.", nameof(filePath));
        Directory.CreateDirectory(directory);
        _watcher = new FileSystemWatcher(directory, Path.GetFileName(filePath))
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };
        _watcher.Changed += FileChanged;
        _watcher.Created += FileChanged;
        _watcher.Deleted += FileChanged;
        _watcher.Renamed += FileChanged;
        _watcher.Error += WatcherError;
    }

    public IDisposable OnChange(Action listener)
    {
        lock (_sync)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
            
            _listeners.Add(listener);
        }
        return new Subscription(this, listener);
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            _listeners.Clear();
        }
        _watcher.Dispose();
    }

    private void FileChanged(object sender, FileSystemEventArgs args) => NotifyListeners();
    private void WatcherError(object sender, ErrorEventArgs args) => NotifyListeners();

    private void NotifyListeners()
    {
        Action[] listeners;
        lock (_sync)
        {
            if (_disposed) return;
            listeners = [.. _listeners];
        }
        foreach (Action listener in listeners) listener();
    }

    private void Unsubscribe(Action listener)
    {
        lock (_sync)
            _listeners.Remove(listener);
    }

    private sealed class Subscription(ConfigurationFileChangeSource owner, Action listener) : IDisposable
    {
        private ConfigurationFileChangeSource? _owner = owner;
        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.Unsubscribe(listener);
    }
}
