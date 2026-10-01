using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.Updater.Core.Abstractions;
using Revit.Linter.Updater.Core.Models;
using Revit.Linter.Updater.Core.Services;

namespace Revit.Linter.Updater.Core.Tests;

public sealed class UpdateCoordinatorTests
{
    private static readonly StableVersion CurrentVersion = new(1, 2, 3);
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CheckAsync_AutomaticCheckBeforeInterval_DoesNotCallGitHub()
    {
        var releases = new FakeReleaseClient(NewRelease());
        var state = new FakeStateStore(new UpdaterState { LastCheckedAt = Now.AddHours(-1) });
        var coordinator = CreateCoordinator(releases, state);

        UpdateCheckResult result = await coordinator.CheckAsync(
            CurrentVersion, UpdateCheckMode.Automatic, TestContext.Current.CancellationToken);

        Assert.Equal(UpdateCheckStatus.NotDue, result.Status);
        Assert.Equal(0, releases.CallCount);
    }

    [Fact]
    public async Task CheckAsync_ManualCheckBypassesInterval()
    {
        var releases = new FakeReleaseClient(NewRelease());
        var state = new FakeStateStore(new UpdaterState { LastCheckedAt = Now.AddHours(-1) });
        var coordinator = CreateCoordinator(releases, state);

        UpdateCheckResult result = await coordinator.CheckAsync(
            CurrentVersion, UpdateCheckMode.Manual, TestContext.Current.CancellationToken);

        Assert.Equal(UpdateCheckStatus.UpdateAvailable, result.Status);
        Assert.Equal(1, releases.CallCount);
    }

    [Fact]
    public async Task CheckAsync_SkippedLatestVersion_ReturnsSkipped()
    {
        var state = new FakeStateStore(new UpdaterState { SkippedVersion = "2.0.0" });
        var coordinator = CreateCoordinator(new FakeReleaseClient(NewRelease()), state);

        UpdateCheckResult result = await coordinator.CheckAsync(
            CurrentVersion, UpdateCheckMode.Automatic, TestContext.Current.CancellationToken);

        Assert.Equal(UpdateCheckStatus.Skipped, result.Status);
    }

    [Fact]
    public async Task CheckAsync_FailedRequest_DoesNotAdvanceLastCheckedAt()
    {
        var initialTime = Now.AddDays(-2);
        var state = new FakeStateStore(new UpdaterState { LastCheckedAt = initialTime });
        var coordinator = CreateCoordinator(new FakeReleaseClient(new HttpRequestException("offline")), state);

        UpdateCheckResult result = await coordinator.CheckAsync(
            CurrentVersion, UpdateCheckMode.Automatic, TestContext.Current.CancellationToken);

        Assert.Equal(UpdateCheckStatus.Failed, result.Status);
        Assert.Equal(initialTime, state.State.LastCheckedAt);
        Assert.Equal(0, state.SaveCount);
    }

    [Fact]
    public async Task CheckAsync_Success_RecordsUtcCheckTime()
    {
        var state = new FakeStateStore(new UpdaterState());
        var coordinator = CreateCoordinator(new FakeReleaseClient((ReleaseInfo?)null), state);

        UpdateCheckResult result = await coordinator.CheckAsync(
            CurrentVersion, UpdateCheckMode.Automatic, TestContext.Current.CancellationToken);

        Assert.Equal(UpdateCheckStatus.UpToDate, result.Status);
        Assert.Equal(Now, state.State.LastCheckedAt);
        Assert.Equal(1, state.SaveCount);
    }

    private static UpdateCoordinator CreateCoordinator(
        IGitHubReleaseClient releaseClient,
        IUpdaterStateStore stateStore) =>
        new(releaseClient, stateStore, new FixedTimeProvider(Now), NullLogger<UpdateCoordinator>.Instance);

    private static ReleaseInfo NewRelease() =>
        new(new StableVersion(2, 0, 0), new Uri("https://example.com/releases/v2.0.0"));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeReleaseClient : IGitHubReleaseClient
    {
        private readonly ReleaseInfo? _release;
        private readonly Exception? _exception;

        public FakeReleaseClient(ReleaseInfo? release) => _release = release;
        public FakeReleaseClient(Exception exception) => _exception = exception;
        public int CallCount { get; private set; }

        public Task<ReleaseInfo?> GetLatestAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return _exception is null
                ? Task.FromResult(_release)
                : Task.FromException<ReleaseInfo?>(_exception);
        }
    }

    private sealed class FakeStateStore(UpdaterState state) : IUpdaterStateStore
    {
        public UpdaterState State { get; private set; } = state;
        public int SaveCount { get; private set; }

        public Task<UpdaterState> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(State);

        public Task SaveAsync(UpdaterState state, CancellationToken cancellationToken = default)
        {
            State = state;
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
