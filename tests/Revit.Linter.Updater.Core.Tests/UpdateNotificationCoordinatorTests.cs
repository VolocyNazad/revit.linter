using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.Updater.Core.Abstractions;
using Revit.Linter.Updater.Core.Models;
using Revit.Linter.Updater.Core.Services;

namespace Revit.Linter.Updater.Core.Tests;

public sealed class UpdateNotificationCoordinatorTests
{
    private static readonly StableVersion CurrentVersion = new(1, 0, 0);
    private static readonly ReleaseInfo Release = new(
        new StableVersion(2, 0, 0),
        new Uri("https://github.com/VolocyNazad/revit.linter/releases/tag/v2.0.0"));

    [Fact]
    public async Task NotifyIfNeededAsync_UpdateAvailable_ShowsAndPersistsVersion()
    {
        var notifications = new FakeNotificationService(true);
        var stateStore = new FakeStateStore();
        var coordinator = CreateCoordinator(notifications, stateStore);

        bool shown = await coordinator.NotifyIfNeededAsync(
            AvailableResult(), TestContext.Current.CancellationToken);

        Assert.True(shown);
        Assert.Equal(1, notifications.CallCount);
        Assert.Equal("2.0.0", stateStore.State.LastNotifiedVersion);
    }

    [Fact]
    public async Task NotifyIfNeededAsync_AlreadyNotified_DoesNotShowAgain()
    {
        var notifications = new FakeNotificationService(true);
        var stateStore = new FakeStateStore { State = { LastNotifiedVersion = "2.0.0" } };
        var coordinator = CreateCoordinator(notifications, stateStore);

        bool shown = await coordinator.NotifyIfNeededAsync(
            AvailableResult(), TestContext.Current.CancellationToken);

        Assert.False(shown);
        Assert.Equal(0, notifications.CallCount);
    }

    [Theory]
    [InlineData(UpdateCheckStatus.UpToDate)]
    [InlineData(UpdateCheckStatus.Skipped)]
    [InlineData(UpdateCheckStatus.Failed)]
    public async Task NotifyIfNeededAsync_NonAvailableResult_DoesNotShow(UpdateCheckStatus status)
    {
        var notifications = new FakeNotificationService(true);
        var coordinator = CreateCoordinator(notifications, new FakeStateStore());
        var result = new UpdateCheckResult(status, CurrentVersion, Release);

        bool shown = await coordinator.NotifyIfNeededAsync(
            result, TestContext.Current.CancellationToken);

        Assert.False(shown);
        Assert.Equal(0, notifications.CallCount);
    }

    [Fact]
    public async Task NotifyIfNeededAsync_DisplayFailure_DoesNotPersistVersion()
    {
        var notifications = new FakeNotificationService(false);
        var stateStore = new FakeStateStore();
        var coordinator = CreateCoordinator(notifications, stateStore);

        bool shown = await coordinator.NotifyIfNeededAsync(
            AvailableResult(), TestContext.Current.CancellationToken);

        Assert.False(shown);
        Assert.Null(stateStore.State.LastNotifiedVersion);
    }

    [Fact]
    public async Task NotifyIfNeededAsync_NotificationException_DegradesToFalse()
    {
        var notifications = new FakeNotificationService(new InvalidOperationException("Unavailable"));
        var coordinator = CreateCoordinator(notifications, new FakeStateStore());

        bool shown = await coordinator.NotifyIfNeededAsync(
            AvailableResult(), TestContext.Current.CancellationToken);

        Assert.False(shown);
    }

    [Fact]
    public async Task NotifyIfNeededAsync_NotificationsDisabled_DoesNotLoadStateOrShow()
    {
        var notifications = new FakeNotificationService(true);
        var stateStore = new FakeStateStore();
        var coordinator = CreateCoordinator(
            notifications,
            stateStore,
            DefaultConfiguration() with { NotificationsEnabled = false });

        bool shown = await coordinator.NotifyIfNeededAsync(
            AvailableResult(), TestContext.Current.CancellationToken);

        Assert.False(shown);
        Assert.Equal(0, notifications.CallCount);
        Assert.Equal(0, stateStore.LoadCount);
    }

    private static UpdateCheckResult AvailableResult() =>
        new(UpdateCheckStatus.UpdateAvailable, CurrentVersion, Release);

    private static UpdateNotificationCoordinator CreateCoordinator(
        IUpdateNotificationService notificationService,
        IUpdaterStateStore stateStore,
        UpdaterConfiguration? configuration = null) =>
        new(
            notificationService,
            stateStore,
            configuration ?? DefaultConfiguration(),
            NullLogger<UpdateNotificationCoordinator>.Instance);

    private static UpdaterConfiguration DefaultConfiguration() => new(
        ChecksEnabled: true,
        AutomaticChecksEnabled: true,
        NotificationsEnabled: true,
        UpdaterConfiguration.DefaultAutomaticCheckInterval,
        UpdaterConfiguration.DefaultReleaseApiUri);

    private sealed class FakeNotificationService : IUpdateNotificationService
    {
        private readonly bool _result;
        private readonly Exception? _exception;

        public FakeNotificationService(bool result) => _result = result;

        public FakeNotificationService(Exception exception) => _exception = exception;

        public int CallCount { get; private set; }

        public Task<bool> TryShowAsync(ReleaseInfo release, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return _exception is null
                ? Task.FromResult(_result)
                : Task.FromException<bool>(_exception);
        }
    }

    private sealed class FakeStateStore : IUpdaterStateStore
    {
        public UpdaterState State { get; } = new();

        public int LoadCount { get; private set; }

        public Task<UpdaterState> LoadAsync(CancellationToken cancellationToken = default)
        {
            LoadCount++;
            return Task.FromResult(State);
        }

        public Task SaveAsync(UpdaterState state, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
