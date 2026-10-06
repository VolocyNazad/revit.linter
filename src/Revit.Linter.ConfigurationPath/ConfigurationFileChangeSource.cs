namespace Revit.Linter.ConfigurationPath;

/// <summary>
/// Notifies subscribers when a configuration file is created, changed, deleted, renamed, or can no longer be watched.
/// </summary>
public sealed class ConfigurationFileChangeSource : IDisposable
{
    private readonly object _sync = new();
    private readonly List<Action> _listeners = [];
    private readonly FileSystemWatcher[] _watchers;
    private bool _disposed;

    /// <summary>
    /// Initializes a change source for a single configuration file and creates its parent directory when necessary.
    /// </summary>
    /// <param name="filePath">The path of the configuration file to watch.</param>
    /// <exception cref="ArgumentException"><paramref name="filePath"/> does not contain a directory.</exception>
    public ConfigurationFileChangeSource(string filePath) : this([filePath])
    {
    }

    /// <summary>
    /// Initializes a change source for several configuration files and creates their parent directories when necessary.
    /// </summary>
    /// <param name="filePaths">The configuration files to watch.</param>
    /// <exception cref="ArgumentException">A path does not contain a directory, or no paths were supplied.</exception>
    public ConfigurationFileChangeSource(IEnumerable<string> filePaths)
    {
        string[] paths = filePaths.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (paths.Length == 0) throw new ArgumentException("At least one configuration file is required.", nameof(filePaths));

        _watchers = paths.Select(CreateWatcher).ToArray();
    }

    /// <summary>
    /// Subscribes a listener to configuration-file notifications.
    /// </summary>
    /// <param name="listener">The callback to invoke when the watched file or watcher state changes.</param>
    /// <returns>A subscription that removes the listener when disposed.</returns>
    /// <exception cref="ObjectDisposedException">The change source has already been disposed.</exception>
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

    /// <summary>
    /// Stops watching the file and removes all listeners.
    /// </summary>
    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            _listeners.Clear();
        }
        foreach (FileSystemWatcher watcher in _watchers) watcher.Dispose();
    }

    private FileSystemWatcher CreateWatcher(string filePath)
    {
        string directory = Path.GetDirectoryName(filePath)
            ?? throw new ArgumentException("Configuration file path must contain a directory.", nameof(filePath));
        Directory.CreateDirectory(directory);
        FileSystemWatcher watcher = new(directory, Path.GetFileName(filePath))
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };
        watcher.Changed += FileChanged;
        watcher.Created += FileChanged;
        watcher.Deleted += FileChanged;
        watcher.Renamed += FileChanged;
        watcher.Error += WatcherError;
        return watcher;
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
