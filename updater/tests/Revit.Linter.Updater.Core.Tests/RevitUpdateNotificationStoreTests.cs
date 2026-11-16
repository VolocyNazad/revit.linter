using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Updater.Core.Services;
using System.Text.Json;

namespace Revit.Linter.Updater.Core.Tests;

public sealed class RevitUpdateNotificationStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "RevitLinterUpdaterTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task WriteAsync_PersistsVersionAndReleasePage()
    {
        string path = Path.Combine(_directory, "result.json");
        var notification = new RevitUpdateNotification(
            "3.2.0",
            "https://github.com/VolocyNazad/revit.linter/releases/tag/v3.2.0");

        await RevitUpdateNotificationStore.WriteAsync(
            path,
            notification,
            TestContext.Current.CancellationToken);

        string json = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        RevitUpdateNotification? read = JsonSerializer.Deserialize<RevitUpdateNotification>(json);
        Assert.Equal(notification, read);
    }

    [Fact]
    public async Task WriteAsync_CreatesMissingDirectory()
    {
        string path = Path.Combine(_directory, "nested", "result.json");
        var notification = new RevitUpdateNotification(
            "3.2.0",
            "https://github.com/VolocyNazad/revit.linter/releases/tag/v3.2.0");

        await RevitUpdateNotificationStore.WriteAsync(
            path,
            notification,
            TestContext.Current.CancellationToken);

        Assert.True(File.Exists(path));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }
}
