using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.Updater.Core.Services;

namespace Revit.Linter.Updater.Core.Tests;

public sealed class UpdateSkipServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "RevitLinterUpdaterTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SkipAsync_ValidVersion_PersistsSkippedVersion()
    {
        string path = Path.Combine(_directory, "state.json");
        var store = new JsonUpdaterStateStore(path);
        var service = new UpdateSkipService(store, NullLogger<UpdateSkipService>.Instance);

        bool skipped = await service.SkipAsync("2.0.0", TestContext.Current.CancellationToken);

        Assert.True(skipped);
        Assert.Equal("2.0.0", (await store.LoadAsync(TestContext.Current.CancellationToken)).SkippedVersion);
    }

    [Fact]
    public async Task SkipAsync_InvalidVersion_DoesNotWriteState()
    {
        string path = Path.Combine(_directory, "state.json");
        var service = new UpdateSkipService(
            new JsonUpdaterStateStore(path),
            NullLogger<UpdateSkipService>.Instance);

        bool skipped = await service.SkipAsync("preview", TestContext.Current.CancellationToken);

        Assert.False(skipped);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task SkipAsync_UnreadableState_ReturnsFalse()
    {
        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, "state.json");
        await File.WriteAllTextAsync(path, "not json", TestContext.Current.CancellationToken);
        var service = new UpdateSkipService(
            new JsonUpdaterStateStore(path),
            NullLogger<UpdateSkipService>.Instance);

        bool skipped = await service.SkipAsync("2.0.0", TestContext.Current.CancellationToken);

        Assert.False(skipped);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }
}
