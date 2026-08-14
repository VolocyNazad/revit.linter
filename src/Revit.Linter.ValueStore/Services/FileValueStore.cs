using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Revit.Linter.ValueStore.Abstractions;

namespace Revit.Linter.ValueStore.Services;

internal sealed class FileValueStore<T> : IValueStore<T>, IDisposable where T : class, new()
{
	private readonly ILogger<FileValueStore<T>> _logger;
	private readonly IValueStoreSerializer _serializer;
	private readonly ValueStoreNotificationHub _notificationHub;
	private readonly object _lock = new();
	private readonly List<Action<T>> _changeHandlers = [];
	private readonly string _filePath;
	private readonly Timer? _pollingTimer;
	private FileSystemWatcher? _watcher;
	private T _value;
	private string? _fileContent;
	private bool _disposed;

	public FileValueStore(
		IOptions<ValueStoreOptions> options,
		IValueStoreSerializer serializer,
		ValueStoreNotificationHub notificationHub,
		ILogger<FileValueStore<T>> logger)
	{
		_logger = logger;
		_serializer = serializer;
		_notificationHub = notificationHub;
		ValueStoreOptions valueStoreOptions = options.Value;
		_filePath = BuildFilePath(valueStoreOptions);
		(_value, _fileContent) = LoadInitialValue();

		if (!valueStoreOptions.WatchForExternalChanges) return;
		if (valueStoreOptions.PollingInterval <= TimeSpan.Zero)
			throw new InvalidOperationException("The value store polling interval must be greater than zero.");

		_pollingTimer = new Timer(
			_ => PollFileSafely(),
			null,
			valueStoreOptions.PollingInterval,
			valueStoreOptions.PollingInterval);
		StartWatcher();
	}

	public T CurrentValue
	{
		get
		{
			lock (_lock) return CloneValue(_value);
		}
	}

	public IDisposable OnChange(Action<T> listener)
	{
		if (listener is null) throw new ArgumentNullException(nameof(listener));
		lock (_lock)
		{
			ThrowIfDisposed();
			_changeHandlers.Add(listener);
		}

		return new DisposableCallback(() =>
		{
			lock (_lock) _changeHandlers.Remove(listener);
		});
	}

	public void Update(Action<T> change)
	{
		if (change is null) throw new ArgumentNullException(nameof(change));
		T valueForNotification;
		Action<T>[] handlers;

		lock (_lock)
		{
			ThrowIfDisposed();
			T snapshot = CloneValue(_value);
			change(snapshot);
			string content = _serializer.Serialize(snapshot);
			Save(content);
			_value = snapshot;
			_fileContent = content;
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

		_pollingTimer?.Dispose();
		_watcher?.Dispose();
	}

	private string BuildFilePath(ValueStoreOptions options)
	{
		if (string.IsNullOrWhiteSpace(options.DirectoryPath))
			throw new InvalidOperationException("A value store directory path must be configured.");

		string? configuredName = options.FileNameFactory?.Invoke(typeof(T));
		string fileName = configuredName ?? typeof(T).GetCustomAttribute<StoreFileAttribute>()?.FileName
			?? $"{typeof(T).Name}{_serializer.FileExtension}";
		if (string.IsNullOrWhiteSpace(fileName) || Path.IsPathRooted(fileName)
			|| !string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal))
			throw new InvalidOperationException($"Invalid value store file name: '{fileName}'.");

		return Path.Combine(options.DirectoryPath, fileName);
	}

	private (T Value, string? Content) LoadInitialValue()
	{
		return TryLoad(out T? value, out string? content)
			? (value, content)
			: (new T(), null);
	}

	private bool TryLoad(out T value, out string? content)
	{
		for (var attempt = 0; attempt < 3; attempt++)
		{
			try
			{
				if (!File.Exists(_filePath))
				{
					value = new T();
					content = null;
					_notificationHub.ReportSuccess(typeof(T));
					return true;
				}

				content = File.ReadAllText(_filePath);
				value = string.IsNullOrWhiteSpace(content)
					? new T()
					: _serializer.Deserialize<T>(content) ?? new T();
				_notificationHub.ReportSuccess(typeof(T));
				return true;
			}
			catch (IOException) when (attempt < 2)
			{
				Thread.Sleep(100);
			}
			catch (Exception exception)
			{
				_logger.LogWarning(exception, "Failed to load value store file: {Path}", _filePath);
				_notificationHub.ReportFailure(typeof(T), _filePath, exception);
				value = null!;
				content = null;
				return false;
			}
		}

		throw new InvalidOperationException("The value store read retry loop completed unexpectedly.");
	}

	private void Save(string content)
	{
		string? directory = Path.GetDirectoryName(_filePath);
		if (directory is null) throw new InvalidOperationException($"Invalid value store path: '{_filePath}'.");
		Directory.CreateDirectory(directory);

		string temporaryPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
		try
		{
			File.WriteAllText(temporaryPath, content);
			if (File.Exists(_filePath)) File.Replace(temporaryPath, _filePath, null);
			else File.Move(temporaryPath, _filePath);
		}
		finally
		{
			if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
		}
	}

	private void StartWatcher()
	{
		try
		{
			string? directory = Path.GetDirectoryName(_filePath);
			if (directory is null) return;
			Directory.CreateDirectory(directory);
			_watcher = new FileSystemWatcher(directory)
			{
				Filter = Path.GetFileName(_filePath),
				EnableRaisingEvents = true,
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
			};
			_watcher.Changed += OnFileChanged;
			_watcher.Created += OnFileChanged;
			_watcher.Deleted += OnFileChanged;
			_watcher.Renamed += OnFileChanged;
			_watcher.Error += OnWatcherError;
		}
		catch (Exception exception)
		{
			_logger.LogWarning(exception, "Failed to watch value store file: {Path}", _filePath);
			_watcher = null;
		}
	}

	private void OnFileChanged(object sender, FileSystemEventArgs args) => PollFileSafely();
	private void OnWatcherError(object sender, ErrorEventArgs args) => PollFileSafely();

	private void PollFileSafely()
	{
		try
		{
			PollFile();
		}
		catch (Exception exception)
		{
			_logger.LogWarning(exception, "Failed to refresh value store file: {Path}", _filePath);
		}
	}

	private void PollFile()
	{
		T valueForNotification;
		Action<T>[] handlers;
		lock (_lock)
		{
			if (_disposed) return;
			if (!TryLoad(out T value, out string? content)) return;
			if (string.Equals(_fileContent, content, StringComparison.Ordinal)) return;
			_value = value;
			_fileContent = content;
			valueForNotification = CloneValue(value);
			handlers = [.. _changeHandlers];
		}

		NotifyHandlers(handlers, valueForNotification);
	}

	private T CloneValue(T value)
	{
		string content = _serializer.Serialize(value);
		return _serializer.Deserialize<T>(content) ?? new T();
	}

	private void NotifyHandlers(IEnumerable<Action<T>> handlers, T value)
	{
		foreach (Action<T> handler in handlers)
		{
			try
			{
				handler(CloneValue(value));
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, "A value store change handler failed for {ValueType}", typeof(T));
			}
		}
	}

	private void ThrowIfDisposed()
	{
		if (_disposed) throw new ObjectDisposedException(GetType().FullName);
	}

	private sealed class DisposableCallback(Action action) : IDisposable
	{
		private int _disposed;

		public void Dispose()
		{
			if (Interlocked.Exchange(ref _disposed, 1) == 0) action();
		}
	}
}