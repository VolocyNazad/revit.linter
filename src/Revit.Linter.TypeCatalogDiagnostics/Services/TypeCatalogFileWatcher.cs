namespace Revit.Linter.TypeCatalogDiagnostics.Services;

/// <summary>
/// Watches a set of type catalog files and reports every change, creation, removal or rename of one
/// of them.
/// </summary>
/// <remarks>
/// The events are raised on file-system threads. A handler only compares paths and must not touch
/// Revit objects, which are available on the Revit thread alone.
/// </remarks>
internal sealed class TypeCatalogFileWatcher : IDisposable
{
    private readonly object _sync = new();
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    /// <summary>Occurs with the catalog path whose file changed.</summary>
    public event Action<string>? Changed;

    /// <summary>Replaces the watched files, keeping the watchers of files that stay in the set.</summary>
    /// <param name="catalogPaths">The full paths of the type catalog files to watch.</param>
    public void ReplaceWatchSet(IEnumerable<string> catalogPaths)
    {
        string[] paths = catalogPaths
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        lock (_sync)
        {
            if (_disposed) return;

            foreach (string watched in _watchers.Keys.ToArray())
            {
                if (paths.Contains(watched, StringComparer.OrdinalIgnoreCase)) continue;
                if (!_watchers.TryGetValue(watched, out FileSystemWatcher? removed)) continue;
                _watchers.Remove(watched);
                removed.Dispose();
            }

            foreach (string path in paths)
            {
                if (_watchers.ContainsKey(path)) continue;

                FileSystemWatcher? watcher = TryCreateWatcher(path);
                if (watcher is not null) _watchers[path] = watcher;
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;

            foreach (FileSystemWatcher watcher in _watchers.Values) watcher.Dispose();
            _watchers.Clear();
        }
    }

    // A folder that no longer exists has nothing to watch: the next rebuild of the set drops it.
    private FileSystemWatcher? TryCreateWatcher(string catalogPath)
    {
        string? directory = Path.GetDirectoryName(catalogPath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return null;

        FileSystemWatcher watcher = new(directory, Path.GetFileName(catalogPath))
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
                | NotifyFilters.CreationTime,
            IncludeSubdirectories = false,
        };
        watcher.Changed += (_, _) => Notify(catalogPath);
        watcher.Created += (_, _) => Notify(catalogPath);
        watcher.Deleted += (_, _) => Notify(catalogPath);
        watcher.Renamed += (_, _) => Notify(catalogPath);
        // A buffer overflow loses events: the file is checked again so a missed change still lands.
        watcher.Error += (_, _) => Notify(catalogPath);
        watcher.EnableRaisingEvents = true;
        return watcher;
    }

    private void Notify(string catalogPath) => Changed?.Invoke(catalogPath);
}
