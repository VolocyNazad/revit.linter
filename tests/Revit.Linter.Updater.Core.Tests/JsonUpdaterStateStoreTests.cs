using System.Text.Json;
using Revit.Linter.Updater.Core.Models;
using Revit.Linter.Updater.Core.Services;

namespace Revit.Linter.Updater.Core.Tests;

public sealed class JsonUpdaterStateStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        nameof(JsonUpdaterStateStoreTests),
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task LoadAsync_MissingFile_ReturnsDefaults()
    {
        var store = CreateStore();

        UpdaterState state = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(state.AutomaticChecksEnabled);
        Assert.True(state.NotificationsEnabled);
        Assert.Null(state.LastCheckedAt);
        Assert.Null(state.LastNotifiedVersion);
        Assert.Null(state.SkippedVersion);
    }

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsAllStateAndCreatesDirectory()
    {
        var expected = new UpdaterState
        {
            AutomaticChecksEnabled = false,
            NotificationsEnabled = false,
            LastCheckedAt = new DateTimeOffset(2026, 10, 1, 7, 0, 0, TimeSpan.Zero),
            LastNotifiedVersion = "2.1.0",
            SkippedVersion = "2.0.0"
        };
        var store = CreateStore();

        await store.SaveAsync(expected, TestContext.Current.CancellationToken);
        UpdaterState actual = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.False(actual.AutomaticChecksEnabled);
        Assert.False(actual.NotificationsEnabled);
        Assert.Equal(expected.LastCheckedAt, actual.LastCheckedAt);
        Assert.Equal(expected.LastNotifiedVersion, actual.LastNotifiedVersion);
        Assert.Equal(expected.SkippedVersion, actual.SkippedVersion);
        Assert.False(File.Exists(StatePath + ".tmp"));
    }

    [Fact]
    public async Task SaveAsync_ExistingFile_ReplacesIt()
    {
        var store = CreateStore();
        await store.SaveAsync(
            new UpdaterState { SkippedVersion = "1.0.0" },
            TestContext.Current.CancellationToken);

        await store.SaveAsync(
            new UpdaterState { SkippedVersion = "2.0.0" },
            TestContext.Current.CancellationToken);
        UpdaterState actual = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal("2.0.0", actual.SkippedVersion);
    }

    [Fact]
    public async Task LoadAsync_InvalidJson_ThrowsJsonException()
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(StatePath, "{ invalid", TestContext.Current.CancellationToken);
        var store = CreateStore();

        await Assert.ThrowsAsync<JsonException>(
            () => store.LoadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LoadAsync_JsonNull_ReturnsDefaults()
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(StatePath, "null", TestContext.Current.CancellationToken);
        var store = CreateStore();

        UpdaterState state = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(state.AutomaticChecksEnabled);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    private string StatePath => Path.Combine(_directory, "state.json");

    private JsonUpdaterStateStore CreateStore() => new(StatePath);
}
