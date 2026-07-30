using System.Reflection;
using Microsoft.Extensions.Logging;
using Revit.Linter.ValueStore.Abstractions;
using Revit.Linter.ValueStore.Abstractions.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Revit.Linter.ValueStore.Services;

internal sealed class YmlFileValueStore<T> : IValueStore<T>, IDisposable where T : class, new()
{
    private static readonly string _filePath = BuildFilePath();
    private readonly ILogger _logger;
    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();
    private readonly object _lock = new();
    private readonly Timer _pollingTimer;
    private readonly List<Action<T>> _changeHandlers = [];
    private FileSystemWatcher? _watcher;
    private T _value;
    private string? _fileContent;
    private bool _disposed;

    public YmlFileValueStore(ILogger<YmlFileValueStore<T>> logger)
    {
        _logger = logger;
        (_value, _fileContent) = LoadOrCreate();
        _pollingTimer = new Timer(_ => PollFileSafely(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        StartWatcher();
    }

    public T CurrentValue
    {
        get
        {
            lock (_lock)
                return CloneValue(_value);
        }
    }

    public IDisposable OnChange(Action<T> listener)
    {
        if (listener is null)
            throw new ArgumentNullException(nameof(listener));

        lock (_lock)
        {
            ThrowIfDisposed();
            _changeHandlers.Add(listener);
        }

        return new DisposableCallback(() =>
        {
            lock (_lock)
                _changeHandlers.Remove(listener);
        });
    }

    public void Update(Action<T> change)
    {
        if (change is null)
            throw new ArgumentNullException(nameof(change));

        T valueForNotification;
        List<Action<T>> handlers;

        lock (_lock)
        {
            ThrowIfDisposed();

            var snapshot = CloneValue(_value);
            change(snapshot);
            var yaml = _serializer.Serialize(snapshot);
            Save(yaml);

            _value = snapshot;
            _fileContent = yaml;
            valueForNotification = CloneValue(snapshot);
            handlers = [.. _changeHandlers];
        }

        NotifyHandlers(handlers, valueForNotification);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            _changeHandlers.Clear();
        }

        _pollingTimer.Dispose();
        _watcher?.Dispose();
    }

    private static string BuildFilePath()
    {
        var attr = typeof(T).GetCustomAttribute<StoreFileAttribute>();
        var fileName = attr?.FileName ?? $"{typeof(T).Name}.yml";

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RevitLinter",
            fileName);
    }

    private void StartWatcher()
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (directory is null) return;

            Directory.CreateDirectory(directory);
            _watcher = new FileSystemWatcher(directory)
            {
                Filter = Path.GetFileName(_filePath),
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            };
            _watcher.Changed += (_, _) => Expedite();
            _watcher.Created += (_, _) => Expedite();
            _watcher.Deleted += (_, _) => Expedite();
            _watcher.Error += (_, _) => Expedite();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to watch settings file: {Path}", _filePath);
            _watcher = null;
        }
    }

    private void Expedite()
    {
        try
        {
            _pollingTimer.Change(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
        }
        catch (ObjectDisposedException)
        {
            // A watcher event raced with disposal.
        }
    }

    private void PollFileSafely()
    {
        try
        {
            PollFile();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while polling settings file: {Path}", _filePath);
        }
    }

    private void PollFile()
    {
        T valueForNotification;
        List<Action<T>> handlers;

        lock (_lock)
        {
            if (_disposed || !TryReadFromFile(out var newValue, out var content)) return;
            if (string.Equals(_fileContent, content, StringComparison.Ordinal)) return;

            _value = newValue;
            _fileContent = content;
            valueForNotification = CloneValue(newValue);
            handlers = [.. _changeHandlers];
        }

        NotifyHandlers(handlers, valueForNotification);
    }

    private bool TryReadFromFile(out T value, out string? content)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    value = new T();
                    content = null;
                    return true;
                }

                using var stream = new FileStream(
                    _filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096);
                using var reader = new StreamReader(stream);
                content = reader.ReadToEnd();
                value = string.IsNullOrWhiteSpace(content)
                    ? new T()
                    : _deserializer.Deserialize<T>(content) ?? new T();
                return true;
            }
            catch (IOException exception)
            {
                if (attempt < 2)
                {
                    Thread.Sleep(100);
                    continue;
                }

                _logger.LogWarning(exception, "Failed to read settings file after retries: {Path}", _filePath);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to deserialize settings file; keeping the last valid value: {Path}", _filePath);
                break;
            }
        }

        value = null!;
        content = null;
        return false;
    }

    private (T Value, string? Content) LoadOrCreate()
    {
        if (TryReadFromFile(out var result, out var content))
        {
            if (content is not null)
                _logger.LogInformation("Loaded settings from {Path}", _filePath);
            else
                _logger.LogDebug("File not found, using defaults: {Path}", _filePath);

            return (result, content);
        }

        _logger.LogWarning("Settings could not be loaded, using defaults: {Path}", _filePath);
        return (new T(), null);
    }

    private static void Save(string yaml)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        var tempPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(tempPath, yaml);
            if (File.Exists(_filePath))
                File.Replace(tempPath, _filePath, null);
            else
                File.Move(tempPath, _filePath);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().FullName);
    }

    private T CloneValue(T value)
    {
        var yaml = _serializer.Serialize(value);
        return _deserializer.Deserialize<T>(yaml) ?? new T();
    }

    private void NotifyHandlers(IEnumerable<Action<T>> handlers, T value)
    {
        foreach (var handler in handlers)
        {
            try
            {
                handler(value);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "A settings change handler failed for {SettingsType}", typeof(T));
            }
        }
    }

    private sealed class DisposableCallback(Action action) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                action();
        }
    }
}
