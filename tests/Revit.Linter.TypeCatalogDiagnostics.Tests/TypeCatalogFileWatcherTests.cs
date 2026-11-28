#pragma warning disable S6966 // Asynchronous file APIs are unavailable in .NET Framework 4.8.

using Revit.Linter.TypeCatalogDiagnostics.Services;

namespace Revit.Linter.TypeCatalogDiagnostics.Tests;

public sealed class TypeCatalogFileWatcherTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(), nameof(TypeCatalogFileWatcherTests), Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Creating_a_watched_catalog_notifies_subscriber()
    {
        Directory.CreateDirectory(_tempDirectory);
        string catalogPath = GetPath("Door.txt");
        using var watcher = new TypeCatalogFileWatcher();
        var notified = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.Changed += _ => notified.TrySetResult(true);
        watcher.ReplaceWatchSet([catalogPath]);

        File.WriteAllText(catalogPath, "Door##Length##Length");

        await WaitOrThrowAsync(notified.Task, TestContext.Current.CancellationToken);
        Assert.True(notified.Task.Status == TaskStatus.RanToCompletion);
    }

    [Fact]
    public async Task Updating_a_watched_catalog_notifies_subscriber()
    {
        Directory.CreateDirectory(_tempDirectory);
        string catalogPath = GetPath("Door.txt");
        File.WriteAllText(catalogPath, "Door##Length##Length");
        using var watcher = new TypeCatalogFileWatcher();
        var notified = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.Changed += _ => notified.TrySetResult(true);
        watcher.ReplaceWatchSet([catalogPath]);

        File.WriteAllText(catalogPath, "Door##Length##Length\nNarrow##Length##Length");

        await WaitOrThrowAsync(notified.Task, TestContext.Current.CancellationToken);
        Assert.True(notified.Task.Status == TaskStatus.RanToCompletion);
    }

    [Fact]
    public async Task Deleting_a_watched_catalog_notifies_subscriber()
    {
        Directory.CreateDirectory(_tempDirectory);
        string catalogPath = GetPath("Door.txt");
        File.WriteAllText(catalogPath, "Door##Length##Length");
        using var watcher = new TypeCatalogFileWatcher();
        var notified = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.Changed += _ => notified.TrySetResult(true);
        watcher.ReplaceWatchSet([catalogPath]);

        File.Delete(catalogPath);

        await WaitOrThrowAsync(notified.Task, TestContext.Current.CancellationToken);
        Assert.True(notified.Task.Status == TaskStatus.RanToCompletion);
    }

    [Fact]
    public async Task A_catalog_outside_the_watch_set_is_not_reported()
    {
        Directory.CreateDirectory(_tempDirectory);
        string doorCatalogPath = GetPath("Door.txt");
        string windowCatalogPath = GetPath("Window.txt");
        File.WriteAllText(windowCatalogPath, "Window##Width##Length");
        using var watcher = new TypeCatalogFileWatcher();
        int doorNotifications = 0;
        watcher.Changed += path =>
        {
            if (string.Equals(path, doorCatalogPath, StringComparison.OrdinalIgnoreCase))
                Interlocked.Increment(ref doorNotifications);
        };
        watcher.ReplaceWatchSet([doorCatalogPath]);

        File.WriteAllText(windowCatalogPath, "Window##Width##Length\nWide##Width##Length");
        await Task.Delay(500, TestContext.Current.CancellationToken);

        Assert.Equal(0, Volatile.Read(ref doorNotifications));
    }

    [Fact]
    public async Task A_catalog_removed_from_the_set_is_not_reported()
    {
        Directory.CreateDirectory(_tempDirectory);
        string doorCatalogPath = GetPath("Door.txt");
        string windowCatalogPath = GetPath("Window.txt");
        File.WriteAllText(doorCatalogPath, "Door##Length##Length");
        File.WriteAllText(windowCatalogPath, "Window##Width##Length");
        using var watcher = new TypeCatalogFileWatcher();
        var windowNotified = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        int doorNotifications = 0;
        watcher.Changed += path =>
        {
            if (string.Equals(path, doorCatalogPath, StringComparison.OrdinalIgnoreCase))
                Interlocked.Increment(ref doorNotifications);
            else if (string.Equals(path, windowCatalogPath, StringComparison.OrdinalIgnoreCase))
                windowNotified.TrySetResult(true);
        };
        watcher.ReplaceWatchSet([doorCatalogPath, windowCatalogPath]);

        watcher.ReplaceWatchSet([windowCatalogPath]);
        File.WriteAllText(doorCatalogPath, "Door##Length##Length\nNarrow##Length##Length");
        File.WriteAllText(windowCatalogPath, "Window##Width##Length\nWide##Width##Length");

        await WaitOrThrowAsync(windowNotified.Task, TestContext.Current.CancellationToken);
        Assert.Equal(0, Volatile.Read(ref doorNotifications));
    }

    [Fact]
    public void A_catalog_in_a_missing_folder_is_skipped()
    {
        using var watcher = new TypeCatalogFileWatcher();

        Exception? exception = Record.Exception(
            () => watcher.ReplaceWatchSet([Path.Combine(_tempDirectory, "missing", "Door.txt")]));

        Assert.Null(exception);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory)) Directory.Delete(_tempDirectory, recursive: true);
    }

    private string GetPath(params string[] parts) => parts.Aggregate(_tempDirectory, Path.Combine);

    private static async Task WaitOrThrowAsync(Task task, CancellationToken cancellationToken)
    {
        if (task != await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken)))
            throw new TimeoutException("The notification was not received within the timeout.");
        await task;
    }
}

#pragma warning restore S6966
