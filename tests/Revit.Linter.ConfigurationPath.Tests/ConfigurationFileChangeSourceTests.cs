#pragma warning disable S6966 // Asynchronous file APIs are unavailable in .NET Framework 4.8.

namespace Revit.Linter.ConfigurationPath.Tests;

public sealed class ConfigurationFileChangeSourceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(), nameof(ConfigurationFileChangeSourceTests), Guid.NewGuid().ToString("N"));

    [Fact]
    public void Constructor_creates_configuration_directory()
    {
        string filePath = GetPath("nested", "configuration.yaml");

        using var source = new ConfigurationFileChangeSource(filePath);

        Assert.True(Directory.Exists(Path.GetDirectoryName(filePath)));
    }

    [Fact]
    public async Task Creating_watched_file_notifies_subscriber()
    {
        string filePath = GetPath("configuration.yaml");
        using var source = new ConfigurationFileChangeSource(filePath);
        var notified = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using IDisposable subscription = source.OnChange(() => notified.TrySetResult(true));

        File.WriteAllText(filePath, "value: changed");

        await WaitOrThrowAsync(notified.Task, TestContext.Current.CancellationToken);
        Assert.True(notified.Task.Status == TaskStatus.RanToCompletion);
    }

    [Fact]
    public async Task Updating_watched_file_notifies_subscriber()
    {
        string filePath = GetPath("configuration.yaml");
        Directory.CreateDirectory(_tempDirectory);
        File.WriteAllText(filePath, "value: initial");
        using var source = new ConfigurationFileChangeSource(filePath);

        await AssertNotification(source, () =>
        {
            File.WriteAllText(filePath, "value: changed");
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task Deleting_watched_file_notifies_subscriber()
    {
        string filePath = GetPath("configuration.yaml");
        Directory.CreateDirectory(_tempDirectory);
        File.WriteAllText(filePath, "value: initial");
        using var source = new ConfigurationFileChangeSource(filePath);

        await AssertNotification(source, () =>
        {
            File.Delete(filePath);
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task Renaming_watched_file_notifies_subscriber()
    {
        string filePath = GetPath("configuration.yaml");
        string renamedPath = GetPath("configuration.backup.yaml");
        Directory.CreateDirectory(_tempDirectory);
        File.WriteAllText(filePath, "value: initial");
        using var source = new ConfigurationFileChangeSource(filePath);

        await AssertNotification(source, () =>
        {
            File.Move(filePath, renamedPath);
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task Disposed_subscription_is_not_notified()
    {
        string filePath = GetPath("configuration.yaml");
        using var source = new ConfigurationFileChangeSource(filePath);
        int notificationCount = 0;
        IDisposable subscription = source.OnChange(() => Interlocked.Increment(ref notificationCount));
        subscription.Dispose();

        File.WriteAllText(filePath, "value: changed");
        await Task.Delay(500, TestContext.Current.CancellationToken);

        Assert.Equal(0, Volatile.Read(ref notificationCount));
    }

    [Fact]
    public void Disposed_source_rejects_new_subscriptions()
    {
        string filePath = GetPath("configuration.yaml");
        var source = new ConfigurationFileChangeSource(filePath);
        source.Dispose();

        Assert.Throws<ObjectDisposedException>(() => source.OnChange(() => { }));
        source.Dispose();
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

    private static async Task AssertNotification(
        ConfigurationFileChangeSource source, Func<Task> change)
    {
        var notified = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using IDisposable subscription = source.OnChange(() => notified.TrySetResult(true));

        await change();
        await WaitOrThrowAsync(notified.Task, TestContext.Current.CancellationToken);

        Assert.True(notified.Task.Status == TaskStatus.RanToCompletion);
    }
}

#pragma warning restore S6966
