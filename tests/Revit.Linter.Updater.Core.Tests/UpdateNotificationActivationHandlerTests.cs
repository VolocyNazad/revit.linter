using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.Updater.Core.Abstractions;
using Revit.Linter.Updater.Core.Models;
using Revit.Linter.Updater.Core.Services;

namespace Revit.Linter.Updater.Core.Tests;

public sealed class UpdateNotificationActivationHandlerTests
{
    private static readonly StableVersion Version = new(2, 0, 0);
    private static readonly Uri ReleasePage = new(
        "https://github.com/VolocyNazad/revit.linter/releases/tag/v2.0.0");

    [Theory]
    [InlineData(UpdateNotificationAction.Download)]
    [InlineData(UpdateNotificationAction.ReleaseNotes)]
    public async Task HandleAsync_NavigationAction_ReturnsTrustedPage(UpdateNotificationAction action)
    {
        var handler = CreateHandler(new FakeStateStore());

        Uri? result = await handler.HandleAsync(
            ParseAction(action),
            TestContext.Current.CancellationToken);

        Assert.Equal(ReleasePage, result);
    }

    [Fact]
    public async Task HandleAsync_SkipVersion_PersistsVersion()
    {
        var store = new FakeStateStore();
        var handler = CreateHandler(store);

        Uri? result = await handler.HandleAsync(
            ParseAction(UpdateNotificationAction.SkipVersion),
            TestContext.Current.CancellationToken);

        Assert.Null(result);
        Assert.Equal("2.0.0", store.State.SkippedVersion);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_Later_ClearsMatchingNotificationMarker()
    {
        var store = new FakeStateStore();
        store.State.LastNotifiedVersion = "2.0.0";
        var handler = CreateHandler(store);

        await handler.HandleAsync(
            ParseAction(UpdateNotificationAction.Later),
            TestContext.Current.CancellationToken);

        Assert.Null(store.State.LastNotifiedVersion);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_Later_DoesNotClearAnotherVersion()
    {
        var store = new FakeStateStore();
        store.State.LastNotifiedVersion = "3.0.0";
        var handler = CreateHandler(store);

        await handler.HandleAsync(
            ParseAction(UpdateNotificationAction.Later),
            TestContext.Current.CancellationToken);

        Assert.Equal("3.0.0", store.State.LastNotifiedVersion);
        Assert.Equal(0, store.SaveCount);
    }

    private static UpdateNotificationActivationHandler CreateHandler(IUpdaterStateStore stateStore) =>
        new(stateStore, NullLogger<UpdateNotificationActivationHandler>.Instance);

    private static UpdateNotificationActivation ParseAction(UpdateNotificationAction action)
    {
        string name = action switch
        {
            UpdateNotificationAction.Download => "download",
            UpdateNotificationAction.ReleaseNotes => "release",
            UpdateNotificationAction.Later => "later",
            UpdateNotificationAction.SkipVersion => "skip",
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
        string arguments = $"action={name}&version={Version}";
        if (action is UpdateNotificationAction.Download or UpdateNotificationAction.ReleaseNotes)
            arguments += "&url=" + Uri.EscapeDataString(ReleasePage.AbsoluteUri);

        Assert.True(UpdateNotificationActivation.TryParse(arguments, out var activation));
        return Assert.IsType<UpdateNotificationActivation>(activation);
    }

    private sealed class FakeStateStore : IUpdaterStateStore
    {
        public UpdaterState State { get; } = new();

        public int SaveCount { get; private set; }

        public Task<UpdaterState> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(State);

        public Task SaveAsync(UpdaterState state, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
