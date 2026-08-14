using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.ValueStore.Abstractions;
using Revit.Linter.ValueStore.DI;
using Revit.Linter.ValueStore.Serialization;

namespace Revit.Linter.ValueStore.Tests;

[StoreFile("custom-settings.yml")]
internal sealed class TestSettings
{
    public string? Name { get; set; }
    public int Count { get; set; }
}

public sealed class FileValueStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), $"Revit.Linter.ValueStore.Tests-{Guid.NewGuid():N}");

    [Fact]
    public void Update_persists_value_immediately()
    {
        using ServiceProvider provider = CreateProvider();
        IValueStore<TestSettings> store = provider.GetRequiredService<IValueStore<TestSettings>>();

        store.Update(settings => settings.Count = 42);

        string content = File.ReadAllText(Path.Combine(_directory, "custom-settings.yml"));
        Assert.Contains("42", content);
    }

    [Fact]
    public void New_provider_loads_persisted_value()
    {
        using (ServiceProvider provider = CreateProvider())
            provider.GetRequiredService<IValueStore<TestSettings>>()
                .Update(settings => settings.Name = "saved");

        using ServiceProvider nextProvider = CreateProvider();
        Assert.Equal("saved", nextProvider.GetRequiredService<IValueStore<TestSettings>>().CurrentValue.Name);
    }

    [Fact]
    public void Current_value_is_a_snapshot()
    {
        using ServiceProvider provider = CreateProvider();
        IValueStore<TestSettings> store = provider.GetRequiredService<IValueStore<TestSettings>>();

        TestSettings snapshot = store.CurrentValue;
        snapshot.Count = 10;

        Assert.Equal(0, store.CurrentValue.Count);
    }

    [Fact]
    public async Task External_change_triggers_notification()
    {
        using ServiceProvider provider = CreateProvider(watch: true);
        IValueStore<TestSettings> store = provider.GetRequiredService<IValueStore<TestSettings>>();
        store.Update(settings => settings.Name = "original");
        var received = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using IDisposable subscription = store.OnChange(_ => received.TrySetResult(true));

        WriteExternalFile("name: modified\ncount: 99");

        Task completed = await Task.WhenAny(
            received.Task,
            Task.Delay(3000, TestContext.Current.CancellationToken));
        Assert.Same(received.Task, completed);
        Assert.Equal("modified", store.CurrentValue.Name);
    }

    [Fact]
    public async Task Invalid_external_content_keeps_last_valid_value()
    {
        using ServiceProvider provider = CreateProvider(watch: true);
        IValueStore<TestSettings> store = provider.GetRequiredService<IValueStore<TestSettings>>();
        store.Update(settings => settings.Name = "valid");

        WriteExternalFile("name: [invalid");
        await Task.Delay(1500, TestContext.Current.CancellationToken);

        Assert.Equal("valid", store.CurrentValue.Name);
    }

    [Fact]
    public async Task Load_retries_when_file_is_temporarily_locked()
    {
        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, "custom-settings.yml");
        File.WriteAllText(path, "name: loaded\ncount: 42");
        using var fileLock = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
        Task releaseLock = Task.Run(async () =>
        {
            await Task.Delay(150, TestContext.Current.CancellationToken);
            fileLock.Dispose();
        }, TestContext.Current.CancellationToken);

        using ServiceProvider provider = CreateProvider();
        TestSettings value = provider.GetRequiredService<IValueStore<TestSettings>>().CurrentValue;
        await releaseLock;

        Assert.Equal(42, value.Count);
        Assert.Equal("loaded", value.Name);
    }

    private ServiceProvider CreateProvider(bool watch = false)
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddValueStore<YamlValueStoreSerializer>(options =>
        {
            options.DirectoryPath = _directory;
            options.WatchForExternalChanges = watch;
        });
        return services.BuildServiceProvider();
    }

    private void WriteExternalFile(string content)
    {
        string path = Path.Combine(_directory, "custom-settings.yml");
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                File.WriteAllText(path, content);
                return;
            }
            catch (IOException) when (attempt < 4)
            {
                Thread.Sleep(50);
            }
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }
}
