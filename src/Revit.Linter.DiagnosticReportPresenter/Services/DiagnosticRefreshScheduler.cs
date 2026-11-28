using System.Windows.Threading;

namespace Revit.Linter.DiagnosticReportPresenter.Services;

/// <summary>
/// Times the diagnostic refreshes of the report and keeps what a run has to check.
/// </summary>
/// <remarks>
/// The scheduler runs on the WPF dispatcher thread: the report queues work there from the Revit thread,
/// which is the same thread, and the timer ticks there. The checks themselves run in a Revit API context
/// through <see cref="IRevitIdlingScheduler"/>.
/// </remarks>
internal sealed class DiagnosticRefreshScheduler(IRevitIdlingScheduler idlingScheduler)
    : IDiagnosticRefreshScheduler, IDisposable
{
    // A run waits for a quiet period, so a burst of changes such as an undo chain is checked once.
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromMilliseconds(250);
    // Continuous editing must not starve the list, so a run happens at least this often.
    private static readonly TimeSpan MaximumDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);
    // Document diagnostics scan the whole document, so they run less often than the element checks.
    private static readonly TimeSpan DocumentDiagnosticsPeriod = TimeSpan.FromSeconds(2);

    private readonly Dictionary<Document, HashSet<ElementId>> _pendingElementRefreshes = [];
    private readonly HashSet<Document> _pendingDocumentRefreshes = [];
    // Elements whose rows the report removed because of an ignore. A non-committed change can revert the
    // ignore, and the storage change never names these elements, so the report has to remember them.
    private readonly Dictionary<Document, HashSet<ElementId>> _ignoredElementIds = [];
    private long _firstPendingChangeTimestamp;
    private long _lastPendingChangeTimestamp;
    private long _lastDocumentDiagnosticsTimestamp;
    private bool _refreshScheduled;
    private bool _paused;
    private DispatcherTimer? _timer;

    public event EventHandler<DiagnosticRefreshRequestedEventArgs>? RefreshRequested;

    public void QueueRefresh(Document document, IReadOnlyCollection<ElementId> elementIds)
    {
        _pendingDocumentRefreshes.Add(document);

        if (elementIds.Count > 0)
        {
            if (_pendingElementRefreshes.Count == 0)
                _firstPendingChangeTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            _lastPendingChangeTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();

            if (!_pendingElementRefreshes.TryGetValue(document, out HashSet<ElementId>? pending))
            {
                pending = [];
                _pendingElementRefreshes[document] = pending;
            }
            pending.UnionWith(elementIds);
        }

        StartTimer();
    }

    public void RememberIgnoredElements(Document document, IReadOnlyCollection<ElementId> elementIds)
    {
        if (elementIds.Count == 0) return;

        if (!_ignoredElementIds.TryGetValue(document, out HashSet<ElementId>? ignored))
        {
            ignored = [];
            _ignoredElementIds[document] = ignored;
        }
        ignored.UnionWith(elementIds);
    }

    public IReadOnlyCollection<ElementId> GetIgnoredElements(Document document)
        => _ignoredElementIds.TryGetValue(document, out HashSet<ElementId>? ignored) ? ignored : [];

    public void SetPaused(bool isPaused)
    {
        _paused = isPaused;
        if (isPaused) ClearPending();
    }

    public void ClearPending()
    {
        _pendingElementRefreshes.Clear();
        _pendingDocumentRefreshes.Clear();
        StopTimer();
    }

    public void Dispose() => StopTimer();

    private void StartTimer()
    {
        if (_timer is not null)
        {
            if (!_timer.IsEnabled) _timer.Start();
            return;
        }

        _timer = new DispatcherTimer(
            PollInterval, DispatcherPriority.Background, OnTimerTick, Dispatcher.CurrentDispatcher);
        _timer.Start();
    }

    private void StopTimer() => _timer?.Stop();

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_refreshScheduled) return;

        long now = System.Diagnostics.Stopwatch.GetTimestamp();
        bool elementRefreshDue = _pendingElementRefreshes.Count > 0 && IsElementRefreshDue(now);
        bool documentRefreshDue = _pendingDocumentRefreshes.Count > 0 && IsDocumentDiagnosticsRefreshDue(now);

        if (!elementRefreshDue && !documentRefreshDue)
        {
            if (_pendingElementRefreshes.Count == 0 && _pendingDocumentRefreshes.Count == 0)
                StopTimer();
            return;
        }

        List<DiagnosticRefreshBatch> batches = CreateBatches(now, elementRefreshDue, documentRefreshDue);
        if (batches.Count == 0)
        {
            CompleteRefresh();
            return;
        }

        _refreshScheduled = true;
        StopTimer();
        _ = idlingScheduler.RunAsync(_ =>
        {
            try
            {
                if (!_paused)
                    RefreshRequested?.Invoke(this, new DiagnosticRefreshRequestedEventArgs(batches));
            }
            finally
            {
                CompleteRefresh();
            }
        });
    }

    private List<DiagnosticRefreshBatch> CreateBatches(
        long now, bool includeElementRefresh, bool includeDocumentDiagnostics)
    {
        List<DiagnosticRefreshBatch> batches = [];

        if (includeElementRefresh)
        {
            foreach (KeyValuePair<Document, HashSet<ElementId>> entry in _pendingElementRefreshes)
            {
                if (entry.Key is not { IsValidObject: true } || entry.Value.Count == 0) continue;

                // The document rows of a failing document diagnostic are replaced by this run.
                bool includeDocuments = includeDocumentDiagnostics && _pendingDocumentRefreshes.Remove(entry.Key);
                batches.Add(new DiagnosticRefreshBatch(entry.Key, [.. entry.Value], includeDocuments));
            }
            _pendingElementRefreshes.Clear();
        }

        if (!includeDocumentDiagnostics) return batches;

        _lastDocumentDiagnosticsTimestamp = now;
        foreach (Document document in _pendingDocumentRefreshes)
        {
            if (document is not { IsValidObject: true }) continue;

            batches.Add(new DiagnosticRefreshBatch(document, [], true));
        }
        _pendingDocumentRefreshes.Clear();
        return batches;
    }

    private bool IsElementRefreshDue(long now)
        => GetElapsed(_lastPendingChangeTimestamp, now) >= QuietPeriod
        || GetElapsed(_firstPendingChangeTimestamp, now) >= MaximumDelay;

    private bool IsDocumentDiagnosticsRefreshDue(long now)
        => _lastDocumentDiagnosticsTimestamp == 0
        || GetElapsed(_lastDocumentDiagnosticsTimestamp, now) >= DocumentDiagnosticsPeriod;

    private static TimeSpan GetElapsed(long from, long to)
        => TimeSpan.FromSeconds((to - from) / (double)System.Diagnostics.Stopwatch.Frequency);

    private void CompleteRefresh()
    {
        _refreshScheduled = false;
        if (_pendingElementRefreshes.Count > 0 || _pendingDocumentRefreshes.Count > 0)
            StartTimer();
        else
            StopTimer();
    }
}
