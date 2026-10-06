namespace Revit.Linter.Updater.Core.Services;

using Revit.Linter.Core.Abstractions;

/// <summary>Coordinates updater processes for the current Windows user.</summary>
/// <remarks>
/// The process that acquires the gate owns update work. A concurrent manual invocation signals that
/// owner instead of starting another release request. Named objects use the local Windows session.
/// </remarks>
public sealed class UpdaterInstanceGate : IDisposable
{
    private readonly Semaphore _semaphore;
    private readonly EventWaitHandle _manualCheckRequested;
    private bool _ownsSemaphore;

    /// <summary>Creates a gate backed by named operating-system synchronization objects.</summary>
    /// <param name="name">A stable, application-specific name without the Windows object namespace prefix.</param>
    public UpdaterInstanceGate(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _semaphore = new Semaphore(1, 1, ProductIdentity.GetLocalMutexName(name));
        _manualCheckRequested = new EventWaitHandle(
            false,
            EventResetMode.AutoReset,
            ProductIdentity.GetLocalMutexName($"{name}{ProductIdentity.UpdaterManualCheckEventSuffix}"));
    }

    /// <summary>Attempts to become the updater process that performs work.</summary>
    /// <returns><see langword="true"/> when this instance acquired exclusive ownership.</returns>
    public bool TryAcquire()
    {
        if (_ownsSemaphore)
            return true;

        _ownsSemaphore = _semaphore.WaitOne(TimeSpan.Zero, false);
        return _ownsSemaphore;
    }

    /// <summary>Notifies the active updater process that a manual check was requested.</summary>
    /// <remarks>Multiple pending signals are intentionally coalesced into one request.</remarks>
    public void RequestManualCheck() => _manualCheckRequested.Set();

    /// <summary>Consumes a pending manual-check request without waiting.</summary>
    /// <returns><see langword="true"/> when another invocation requested a manual check.</returns>
    public bool ConsumeManualCheckRequest() => _manualCheckRequested.WaitOne(TimeSpan.Zero, false);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsSemaphore)
        {
            _semaphore.Release();
            _ownsSemaphore = false;
        }

        _manualCheckRequested.Dispose();
        _semaphore.Dispose();
    }
}
