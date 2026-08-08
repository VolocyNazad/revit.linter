#pragma warning disable S6966 // Asynchronous file APIs are unavailable in .NET Framework 4.8.

using Microsoft.Extensions.Logging;
using Revit.Linter.ValueStore.Abstractions;
using Revit.Linter.ValueStore.Services;

namespace Revit.Linter.ValueStore.Tests;

[StoreFile("TestSettings.yml")]
internal sealed class TestSettings
{
    public string? Name { get; set; }
    public int Count { get; set; }
}

public sealed class YmlFileValueStoreTests : IDisposable
{
    private readonly string _filePath;
    private readonly string _backupPath;
    private readonly bool _hasBackup;

    public YmlFileValueStoreTests()
    {
        _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RevitLinter",
            $"{nameof(TestSettings)}.yml");

        var dir = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(dir);

        if (File.Exists(_filePath))
        {
            _backupPath = _filePath + ".bak";
            File.Move(_filePath, _backupPath);
            _hasBackup = true;
        }
        else
        {
            _backupPath = null!;
            _hasBackup = false;
        }
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
        }
        catch { }

        if (_hasBackup && File.Exists(_backupPath))
            File.Move(_backupPath, _filePath);
    }

    private static YmlFileValueStore<TestSettings> CreateStore()
    {
        return new YmlFileValueStore<TestSettings>(
            NullLoggerStub<TestSettings>.Instance,
            new ValueStoreNotificationHub());
    }

    private sealed class NullLoggerStub<T> : ILogger<YmlFileValueStore<T>> where T : class, new()
    {
        public static readonly NullLoggerStub<T> Instance = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    [Fact]
    public void CurrentValue_returns_defaults_on_empty_store()
    {
        using var store = CreateStore();

        Assert.Null(store.CurrentValue.Name);
        Assert.Equal(0, store.CurrentValue.Count);
    }

    [Fact]
    public void Update_and_reload_persists_values()
    {
        using (var store = CreateStore())
        {
            store.Update(s => { s.Name = "hello"; s.Count = 42; });
        }

        using (var reloaded = CreateStore())
        {
            Assert.Equal("hello", reloaded.CurrentValue.Name);
            Assert.Equal(42, reloaded.CurrentValue.Count);
        }
    }

    [Fact]
    public void Loads_values_from_existing_file()
    {
        var dir = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(_filePath, "name: hello\ncount: 42");

        using var store = CreateStore();

        Assert.Equal("hello", store.CurrentValue.Name);
        Assert.Equal(42, store.CurrentValue.Count);
    }

    [Fact]
    public async Task File_modified_externally_triggers_OnChange()
    {
        using var store = CreateStore();
        store.Update(s => s.Name = "original");

        var received = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = store.OnChange(_ => received.TrySetResult(true));

        File.WriteAllText(_filePath, "name: modified\ncount: 99");

        var timeout = Task.Delay(3000, TestContext.Current.CancellationToken);
        var completed = await Task.WhenAny(received.Task, timeout);
        Assert.True(completed == received.Task, "OnChange not fired within timeout");
        Assert.Equal("modified", store.CurrentValue.Name);
        Assert.Equal(99, store.CurrentValue.Count);
    }

    [Fact]
    public async Task File_deleted_externally_resets_to_defaults()
    {
        using var store = CreateStore();
        store.Update(s => s.Name = "value");

        var received = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = store.OnChange(_ => received.TrySetResult(true));

        File.Delete(_filePath);

        var timeout = Task.Delay(3000, TestContext.Current.CancellationToken);
        var completed = await Task.WhenAny(received.Task, timeout);
        Assert.True(completed == received.Task, "OnChange not fired within timeout");
        Assert.Null(store.CurrentValue.Name);
    }

    [Fact]
    public void Update_recreates_file_after_external_deletion()
    {
        using var store = CreateStore();
        store.Update(s => s.Name = "value");
        File.Delete(_filePath);

        store.Update(s => s.Name = "newValue");

        Assert.True(File.Exists(_filePath));

        using var reloaded = CreateStore();
        Assert.Equal("newValue", reloaded.CurrentValue.Name);
    }

    [Fact]
    public void Update_notifies_listeners()
    {
        using var store = CreateStore();
        TestSettings? captured = null;
        using var sub = store.OnChange(s => captured = s);

        store.Update(s => s.Name = "test");

        Assert.NotNull(captured);
        Assert.Equal("test", captured.Name);
    }

    [Fact]
    public void OnChange_dispose_stops_notifications()
    {
        using var store = CreateStore();
        var callCount = 0;
        var sub = store.OnChange(_ => callCount++);

        store.Update(s => s.Name = "first");
        sub.Dispose();
        store.Update(s => s.Name = "second");

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task Concurrent_Update_and_read_does_not_corrupt()
    {
        using var store = CreateStore();

        var writeTask = Task.Run(() =>
        {
            for (int i = 0; i < 30; i++)
            {
                var captured = i;
                store.Update(s => s.Count = captured);
                Thread.Yield();
            }
        }, TestContext.Current.CancellationToken);

        var readTask = Task.Run(() =>
        {
            for (int i = 0; i < 30; i++)
            {
                _ = store.CurrentValue.Count;
                Thread.Yield();
            }
        }, TestContext.Current.CancellationToken);

        await Task.WhenAll(writeTask, readTask);

        Assert.InRange(store.CurrentValue.Count, 0, 29);
    }

    [Fact]
    public async Task Unchanged_file_does_not_trigger_OnChange()
    {
        using var store = CreateStore();
        store.Update(s => s.Name = "stable");
        var callCount = 0;
        using var sub = store.OnChange(_ => Interlocked.Increment(ref callCount));

        await Task.Delay(1500, TestContext.Current.CancellationToken);

        Assert.Equal(0, callCount);
    }

    [Fact]
    public async Task Invalid_external_content_keeps_last_valid_value()
    {
        using var store = CreateStore();
        store.Update(s => s.Name = "valid");

        File.WriteAllText(_filePath, "name: [invalid");
        await Task.Delay(1500, TestContext.Current.CancellationToken);

        Assert.Equal("valid", store.CurrentValue.Name);
    }

    [Fact]
    public async Task Concurrent_updates_are_serialized()
    {
        using var store = CreateStore();

        var updates = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => store.Update(s => s.Count++)));
        await Task.WhenAll(updates);

        Assert.Equal(20, store.CurrentValue.Count);
        using var reloaded = CreateStore();
        Assert.Equal(20, reloaded.CurrentValue.Count);
    }

    [Fact]
    public void CurrentValue_is_a_snapshot()
    {
        using var store = CreateStore();
        store.Update(s => s.Name = "stored");

        store.CurrentValue.Name = "not stored";

        Assert.Equal("stored", store.CurrentValue.Name);
    }

    [Fact]
    public void Failing_listener_does_not_prevent_other_listeners()
    {
        using var store = CreateStore();
        var notified = false;
        using var failing = store.OnChange(_ => throw new InvalidOperationException("Test exception"));
        using var succeeding = store.OnChange(_ => notified = true);

        store.Update(s => s.Name = "updated");

        Assert.True(notified);
    }
}

#pragma warning restore S6966
