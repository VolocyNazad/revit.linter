using Microsoft.Extensions.Logging;

namespace Revit.Linter.Diagnostic.Services;

internal sealed class DiagnosticCatalog : IDiagnosticCatalog, IDisposable
{
    private readonly object _sync = new();
    private readonly object _refreshSync = new();
    private SnapshotEntry? _current;
    private readonly IDiagnosticCatalogSnapshotFactory _snapshotFactory;
    private readonly ILogger<DiagnosticCatalog> _logger;
    private readonly DebouncedAction _refreshScheduler;
    private readonly IDisposable[] _changeSubscriptions;
    private long _version = 1;

    public DiagnosticCatalog(
        IDiagnosticCatalogSnapshotFactory snapshotFactory,
        IEnumerable<IDiagnosticCatalogChangeSource> changeSources,
        ILogger<DiagnosticCatalog> logger)
    {
        _snapshotFactory = snapshotFactory;
        _logger = logger;
        _current = new SnapshotEntry(snapshotFactory.Create(), _version);
        _refreshScheduler = new DebouncedAction(TimeSpan.FromMilliseconds(300), RefreshSafely);
        _changeSubscriptions = changeSources
            .Select(source => source.OnChange(ScheduleRefresh))
            .ToArray();
    }

    public event EventHandler<DiagnosticCatalogChangedEventArgs>? Changed;
    public event EventHandler<DiagnosticCatalogRefreshFailedEventArgs>? RefreshFailed;

    public IDiagnosticCatalogSnapshotLease AcquireSnapshot()
    {
        lock (_sync)
        {
            if (_current is null)
                throw new ObjectDisposedException(GetType().FullName);
            return _current.AcquireLease();
        }
    }

    public void Refresh() => Refresh(DiagnosticCatalogChangeOrigin.Manual);

    private void Refresh(DiagnosticCatalogChangeOrigin origin)
    {
        lock (_refreshSync)
        {
            lock (_sync)
                if (_current is null)
                    throw new ObjectDisposedException(GetType().FullName);

            DiagnosticCatalogSnapshotOwner owner = _snapshotFactory.Create();
            SnapshotEntry? previous;
            long version;
            lock (_sync)
            {
                if (_current is null)
                {
                    owner.Dispose();
                    throw new ObjectDisposedException(nameof(DiagnosticCatalog));
                }

                version = ++_version;
                previous = _current;
                _current = new SnapshotEntry(owner, version);
            }

            previous.Retire();
            NotifyChanged(version, origin);
        }
    }

    public void Dispose()
    {
        lock (_refreshSync)
        {
            _refreshScheduler.Dispose();
            foreach (IDisposable subscription in _changeSubscriptions)
                subscription.Dispose();

            SnapshotEntry? current;
            lock (_sync)
            {
                current = _current;
                _current = null;
            }
            current?.Retire();
        }
    }

    private void ScheduleRefresh()
    {
        try
        {
            _refreshScheduler.Schedule();
        }
        catch (ObjectDisposedException)
        {
            // A file-system event raced with catalog disposal.
        }
    }

    private void RefreshSafely()
    {
        try
        {
            Refresh(DiagnosticCatalogChangeOrigin.ExternalFile);
        }
        catch (ObjectDisposedException)
        {
            // A queued refresh raced with catalog disposal.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to refresh the diagnostic catalog; keeping the current snapshot");
            NotifyRefreshFailed(exception);
        }
    }

    private void NotifyRefreshFailed(Exception exception)
    {
        var args = new DiagnosticCatalogRefreshFailedEventArgs(exception);
        Delegate[] handlers = RefreshFailed?.GetInvocationList() ?? [];
        foreach (Delegate handler in handlers)
        {
            try
            {
                ((EventHandler<DiagnosticCatalogRefreshFailedEventArgs>)handler)(this, args);
            }
            catch (Exception handlerException)
            {
                _logger.LogError(handlerException, "A diagnostic catalog refresh failure handler failed");
            }
        }
    }

    private void NotifyChanged(long version, DiagnosticCatalogChangeOrigin origin)
    {
        var args = new DiagnosticCatalogChangedEventArgs(version, origin);
        Delegate[] handlers = Changed?.GetInvocationList() ?? [];
        foreach (Delegate handler in handlers)
        {
            try
            {
                ((EventHandler<DiagnosticCatalogChangedEventArgs>)handler)(this, args);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "A diagnostic catalog change handler failed for version {Version}", version);
            }
        }
    }

    /// <summary>
    /// Owns one catalog snapshot. The initial reference belongs to the catalog,
    /// and every active lease adds another reference. Retiring the entry releases
    /// the catalog reference; resources are disposed after the final lease is returned.
    /// </summary>
    private sealed class SnapshotEntry(DiagnosticCatalogSnapshotOwner owner, long version)
    {
        private int _ownerAndLeaseCount = 1;

        public IDiagnosticCatalogSnapshotLease AcquireLease()
        {
            Interlocked.Increment(ref _ownerAndLeaseCount);
            return new SnapshotLease(this, owner.Snapshot, version);
        }

        public void Retire() => ReleaseReference();

        public void ReleaseLease() => ReleaseReference();

        private void ReleaseReference()
        {
            if (Interlocked.Decrement(ref _ownerAndLeaseCount) == 0)
                owner.Dispose();
        }
    }

    private sealed class SnapshotLease(
        SnapshotEntry owner, DiagnosticCatalogSnapshot snapshot, long version) : IDiagnosticCatalogSnapshotLease
    {
        private SnapshotEntry? _owner = owner;

        public long Version { get; } = version;
        public DiagnosticCatalogSnapshot Snapshot { get; } = snapshot;

        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.ReleaseLease();
    }
}
