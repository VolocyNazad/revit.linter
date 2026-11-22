using Autodesk.Revit.DB.Events;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Revit.Context.Abstractions.Services;
using Revit.Linter.Diagnostic.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.DiagnosticReportPresenter.Interactions.Abstractions.Services;
using Revit.Linter.DiagnosticReportPresenter.Infrastructure;
using Revit.Linter.OpenedDocuments.Abstractions.Services;
using Revit.Linter.DiagnosticReportExporting.Abstractions.Services;
using Revit.Linter.Presentation.Abstractions.Services;
using Revit.Linter.Presentation.Models;
using Revit.Linter.Presentation.ViewModels;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Services;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementChangesProvider.Abstractions.Models;
using Revit.Linter.ElementChangesProvider.Abstractions.Services;
using Revit.Linter.ElementIgnoring.Abstractions.Models;
using Revit.Linter.ElementIgnoring.Abstractions.Services;
using Revit.Linter.FixReportProvider.Abstractions.Services;
using Revit.Linter.Localization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Threading;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels;

// Feature-specific members live in partial files next to this one: .Navigation.cs for stepping through
// findings, .Commands.cs for clipboard and export, .Visualization.cs for pipelines and .Fixes.cs for fixes.
internal sealed partial class DiagnosticReportViewModel : IDiagnosticReportPresenter
{
    public void Clear()
    {
        RestoreActiveVisualizationOrSchedule();
        RestoreHeatmapOrSchedule();
        Collection.Clear();
        ClearFilters();
    }

    public void Clear(string documentTitle)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        RestoreActiveVisualizationOrSchedule();
        RestoreHeatmapOrSchedule();
        List<DiagnosticReportItemViewModel> remaining =
            Collection.Where(i => i.DocumentTitle != documentTitle).ToList();
        int removedCount = Collection.Count - remaining.Count;

        // Removing items one by one makes the sorted collection view search for and handle every item
        // separately, which takes quadratic time on a large report. The items are dropped in one
        // operation instead: a reset when nothing remains, a new collection when other documents' items do.
        if (remaining.Count == 0)
            Collection.Clear();
        else if (removedCount > 0)
            Collection = new BatchObservableCollection<DiagnosticReportItemViewModel>(remaining);

        ClearFilters();
        _logger.LogDebug(
            "Cleared {ReportCount} diagnostic reports of {DocumentTitle} in {ElapsedMilliseconds} ms",
            removedCount, documentTitle, stopwatch.ElapsedMilliseconds);
    }


    /// <inheritdoc />
    public void ClearFindings(string diagnosticCode)
    {
        if (string.IsNullOrWhiteSpace(diagnosticCode))
            throw new ArgumentException("The diagnostic code is required.", nameof(diagnosticCode));

        RestoreHeatmapOrSchedule();
        List<DiagnosticReportItemViewModel> remaining = Collection
            .Where(item => !string.Equals(item.Code, diagnosticCode, StringComparison.Ordinal))
            .ToList();
        int removedCount = Collection.Count - remaining.Count;
        if (removedCount == 0) return;

        if (string.Equals(_activeVisualizationPipeline?.Identity.Code, diagnosticCode, StringComparison.Ordinal))
            RestoreActiveVisualizationOrSchedule();

        // Removing items one by one makes the sorted collection view handle every item separately, which
        // takes quadratic time on a large report; the rows are dropped in one operation instead.
        if (remaining.Count == 0)
            Collection.Clear();
        else
            Collection = new BatchObservableCollection<DiagnosticReportItemViewModel>(remaining);

        ClearFilters();
        _logger.LogInformation(
            "Cleared {ReportCount} findings of diagnostic {DiagnosticCode}", removedCount, diagnosticCode);
    }

    public int Refresh(string documentTitle)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        if (_batchRowCount > 0)
        {
            double ticksPerMillisecond = System.Diagnostics.Stopwatch.Frequency / 1000.0;
            _logger.LogDebug(
                "Diagnostic report rows received in batches: {RowCount}; creating the rows took {CreationMilliseconds:F0} ms, adding them to the list took {AdditionMilliseconds:F0} ms",
                _batchRowCount, _batchCreationTicks / ticksPerMillisecond, _batchAdditionTicks / ticksPerMillisecond);
            _batchRowCount = 0;
            _batchCreationTicks = 0;
            _batchAdditionTicks = 0;
        }

        RefreshFilters();
        _logger.LogDebug(
            "Refreshed the diagnostic report view with {ReportCount} reports in {ElapsedMilliseconds} ms",
            Collection.Count, stopwatch.ElapsedMilliseconds);
        return Collection.Count(item => string.Equals(item.DocumentTitle, documentTitle, StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public void SetDocumentChangesRefresh(bool isEnabled)
    {
        if (_documentChangesRefreshEnabled == isEnabled) return;

        _documentChangesRefreshEnabled = isEnabled;
        if (!isEnabled)
        {
            _pendingElementRefreshes.Clear();
            _pendingDocumentRefreshes.Clear();
            StopDiagnosticRefreshTimer();
        }

        _logger.LogDebug(
            "Diagnostic report updates on document changes are {State}",
            isEnabled ? "enabled" : "paused");
    }
}

[GenerateLocalizedProperties]
internal sealed partial class DiagnosticReportViewModel : RevitInteractionViewModel
{
    private readonly IRevitContext _revitContext;
    private readonly IFixReportSender _fixReportSender;
    private readonly IEnumerable<IAccentElementsService> _accentElementsServices;
    private readonly IDiagnosticReportReceiver _diagnosticReportReceiver;
    private readonly IElementChangesReceiver _elementChangesReceiver;
    private readonly IDiagnosticCatalog _diagnosticCatalog;
    private readonly IDiagnosticService _diagnosticService;
    private readonly IIgnoreElementProvider _ignoreElementProvider;
    private readonly IIgnoreListManager _ignoreListManager;
    private readonly IDialog _dialog;
    private readonly IDocumentationLauncher _documentationLauncher;
    private readonly IUserInterfaceActivityStream _activityStream;
    private readonly IVisualizationViewProvider _visualizationViewProvider;
    private readonly IElementHeatmapService _heatmapService;
    private readonly ILogger<DiagnosticReportViewModel> _logger;
    private readonly IConfirmationDialog _confirmationDialog;
    private readonly IDiagnosticReportExportService _exportService;
    private readonly IClipboardService _clipboardService;
    private readonly IRevitIdlingScheduler _idlingScheduler;
    private readonly ITableColumnsService _tableColumnsService;
    private FilteredRowCollection<DiagnosticReportItemViewModel>? _rows;
    private TableColumnsViewModel? _columns;
    private DiagnosticSearchQuery? _searchQuery;
    private IDiagnosticCatalogSnapshotLease? _catalogLease;
    // Tags are looked up per report row; a code-keyed index keeps row creation O(1) on large reports.
    private readonly Dictionary<string, IReadOnlyList<string>> _tagsByCode = new(StringComparer.Ordinal);
    private IElementVisualizationPipeline? _activeVisualizationPipeline;
    private ElementId? _activeVisualizationTargetId;
    private int _visualizationMutationDepth;
    private bool _catalogChangesEnabled;
    private Dispatcher? _dispatcher;
    private ReportRowAgeTicker? _rowAgeTicker;
    private readonly object _restoreScheduleSync = new();
    private Task? _scheduledRestore;

    // A run waits for a quiet period, so a burst of changes such as an undo chain is checked once.
    private static readonly TimeSpan DiagnosticRefreshQuietPeriod = TimeSpan.FromMilliseconds(250);
    // Continuous editing must not starve the list, so a run happens at least this often.
    private static readonly TimeSpan DiagnosticRefreshMaximumDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan DiagnosticRefreshPollInterval = TimeSpan.FromMilliseconds(100);
    // Document diagnostics scan the whole document, so they run less often than the element checks.
    private static readonly TimeSpan DocumentDiagnosticsRefreshPeriod = TimeSpan.FromSeconds(2);

    private bool _elementChangesEnabled;
    private bool _documentChangesRefreshEnabled = true;
    private bool _diagnosticRefreshScheduled;
    private readonly Dictionary<Document, HashSet<ElementId>> _pendingElementRefreshes = [];
    private readonly HashSet<Document> _pendingDocumentRefreshes = [];
    // Elements whose rows the pane removed because of an ignore. A non-committed change can revert the
    // ignore, and the storage change never names these elements, so the pane has to remember them.
    private readonly Dictionary<Document, HashSet<ElementId>> _ignoredElementIds = [];
    private long _firstPendingChangeTimestamp;
    private long _lastPendingChangeTimestamp;
    private long _lastDocumentDiagnosticsTimestamp;
    private DispatcherTimer? _diagnosticRefreshTimer;

    public DiagnosticReportViewModel(
            IRevitContext revitContext, IRevitIdlingScheduler idlingScheduler, IRevitEventSource revitEventSource,
            IEnumerable<IAccentElementsService> accentElementsServices,
            IFixReportSender fixReportSender,
            IDiagnosticReportReceiver diagnosticReportReceiver, IElementChangesReceiver elementChangesReceiver,
            IDiagnosticCatalog diagnosticCatalog,
            IDiagnosticService diagnosticService, IIgnoreElementProvider ignoreElementProvider,
            IIgnoreListManager ignoreListManager,
            IDialog dialog, IConfirmationDialog confirmationDialog,
            IDocumentationLauncher documentationLauncher,
            IUserInterfaceActivityStream activityStream,
            IVisualizationViewProvider visualizationViewProvider,
            IElementHeatmapService heatmapService,
            IDiagnosticReportExportService exportService, IClipboardService clipboardService,
            IOpenedDocuments openedDocuments,
            ITableColumnsService tableColumnsService,
            ILogger<DiagnosticReportViewModel> logger) : base(revitEventSource)
    {
        _accentElementsServices = accentElementsServices;
        _diagnosticReportReceiver = diagnosticReportReceiver;
        _elementChangesReceiver = elementChangesReceiver;
        _revitContext = revitContext;
        _idlingScheduler = idlingScheduler;
        _fixReportSender = fixReportSender;
        _diagnosticCatalog = diagnosticCatalog;
        _diagnosticService = diagnosticService;
        _ignoreElementProvider = ignoreElementProvider;
        _ignoreListManager = ignoreListManager;
        _dialog = dialog;
        _documentationLauncher = documentationLauncher;
        _activityStream = activityStream;
        _visualizationViewProvider = visualizationViewProvider;
        _heatmapService = heatmapService;
        _logger = logger;
        _confirmationDialog = confirmationDialog;
        _exportService = exportService;
        _clipboardService = clipboardService;
        _tableColumnsService = tableColumnsService;
        OpenedDocuments = openedDocuments;

        Collection = new BatchObservableCollection<DiagnosticReportItemViewModel>();
    }

    /// <summary>
    /// Gets the opened-document selector displayed by the view.
    /// </summary>
    public IOpenedDocuments OpenedDocuments { get; }

    [ObservableProperty]
    public partial ObservableCollection<DiagnosticReportItemViewModel> Collection { get; private set; }
    partial void OnCollectionChanged(ObservableCollection<DiagnosticReportItemViewModel> value)
    {
        Rows.Attach(value);
        CollectionViewSource = Rows.ViewSource;
    }

    [ObservableProperty]
    public partial CollectionViewSource? CollectionViewSource { get; private set; }

    /// <summary>Gets the popup that hides and shows the data columns of the table.</summary>
    /// <remarks>The action columns have no key, so they are not listed and always stay visible.</remarks>
    public TableColumnsViewModel Columns => _columns ??= new(
        TableColumnIds.DiagnosticReport,
        _tableColumnsService,
        [
            new(TableColumnKeys.Code, () => CodeHeader),
            new(TableColumnKeys.Message, () => MessageHeader),
            new(TableColumnKeys.Document, () => DocumentHeader),
            new(TableColumnKeys.Created, () => CreatedHeader),
            new(TableColumnKeys.Tags, () => TagsHeader),
        ]);

    [ObservableProperty]
    public partial string SearchField { get; set; } = string.Empty;
    partial void OnSearchFieldChanged(string value)
    {
        _searchQuery = null;
        Rows.Refresh();
    }

    [ObservableProperty]
    public partial IEnumerable<IDiagnosticFilter<DiagnosticReportItemViewModel>> SeverityFilters { get; set; } = [];
    partial void OnSeverityFiltersChanged(
        IEnumerable<IDiagnosticFilter<DiagnosticReportItemViewModel>> oldValue,
        IEnumerable<IDiagnosticFilter<DiagnosticReportItemViewModel>> newValue)
        => Rows.SetFilters("severity", newValue);

    [ObservableProperty]
    public partial IEnumerable<IDiagnosticFilter<DiagnosticReportItemViewModel>> Filters { get; set; } = [];
    partial void OnFiltersChanged(
        IEnumerable<IDiagnosticFilter<DiagnosticReportItemViewModel>> oldValue,
        IEnumerable<IDiagnosticFilter<DiagnosticReportItemViewModel>> newValue)
        => Rows.SetFilters("flags", newValue);

    [ObservableProperty]
    public partial string? TargetDocumentTitle { get; set; }
    partial void OnTargetDocumentTitleChanged(string? value) => Rows.Refresh();

    [ObservableProperty]
    public partial DiagnosticReportItemViewModel? SelectedReport { get; set; }
    partial void OnSelectedReportChanged(DiagnosticReportItemViewModel? value)
    {
        if (value is not null) _activityStream.Publish(new FindingSelectedActivity(value.Code));
    }

    private void SelectElement(ElementId elementId)
    {
        Document? targetdocument = _revitContext.ActiveDocument;
        if (targetdocument is null) return;

        _accentElementsServices.SelectElement(targetdocument, elementId);
    }


    private FilteredRowCollection<DiagnosticReportItemViewModel> Rows => _rows ??=
        new(
            [new SortDescription(nameof(DiagnosticReportItemViewModel.CodeSortKey), ListSortDirection.Ascending)],
            IsRowVisible);

    private DiagnosticSearchQuery SearchQuery => _searchQuery ??= DiagnosticSearchQuery.Parse(SearchField);

    private bool IsRowVisible(DiagnosticReportItemViewModel viewModel)
        => (string.IsNullOrEmpty(TargetDocumentTitle) || string.Equals(TargetDocumentTitle, viewModel.DocumentTitle))
        && SeverityFilters.Any(filter => filter.IsActive && filter.IsValid(viewModel))
        && Filters.Any(filter => filter.IsActive && filter.IsValid(viewModel))
        && SearchQuery.Matches(viewModel.Code, viewModel.MessageText, viewModel.Tags);

    private void ClearFilters()
    {
        SeverityFilters = [];
        Filters = [];
    }

    private void RefreshFilters()
    {
        // One pass instead of five Collection.Count(predicate) scans; the predicates passed
        // to the filters below stay unchanged, so matching behavior is identical.
        int messageCount = 0;
        int warningCount = 0;
        int errorCount = 0;
        int obsoleteCount = 0;
        int actualCount = 0;
        foreach (DiagnosticReportItemViewModel item in Collection)
        {
            switch (item.Severity)
            {
                case DiagnosticSeverity.Message: messageCount++; break;
                case DiagnosticSeverity.Warning: warningCount++; break;
                case DiagnosticSeverity.Error: errorCount++; break;
            }
            if (item.IsObsolete) obsoleteCount++;
            else actualCount++;
        }

        List<IDiagnosticFilter<DiagnosticReportItemViewModel>> severityFilters = [];

        severityFilters.Add(
            new DiagnosticSeverityFilterViewModel(true, DiagnosticSeverity.Message, messageCount));
        severityFilters.Add(
            new DiagnosticSeverityFilterViewModel(true, DiagnosticSeverity.Warning, warningCount));
        severityFilters.Add(
            new DiagnosticSeverityFilterViewModel(true, DiagnosticSeverity.Error, errorCount));

        SeverityFilters = severityFilters;

        List<IDiagnosticFilter<DiagnosticReportItemViewModel>> filters = [];

        filters.Add(new FilterViewModel<DiagnosticReportItemViewModel, bool>(
            true,
            true,
            obsoleteCount,
            item => item.IsObsolete,
            () => DiagnosticReportFilterLocalizations.GetString("Obsolete")));
        filters.Add(new FilterViewModel<DiagnosticReportItemViewModel, bool>(
            true,
            false,
            actualCount,
            item => !item.IsObsolete,
            () => DiagnosticReportFilterLocalizations.GetString("Actual")));

        Filters = filters;
    }

    protected async override Task OnInitializing(CancellationToken cancellationToken = default)
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        await base.OnInitializing(cancellationToken);
        // The rows outlive the run that created them, so their relative creation time is refreshed
        // while the pane is open instead of freezing at the moment each cell first appeared.
        _rowAgeTicker = new ReportRowAgeTicker(RefreshRowAges, _dispatcher);
        IDiagnosticCatalogSnapshotLease lease = _diagnosticCatalog.AcquireSnapshot();
        _catalogLease?.Dispose();
        _catalogLease = lease;
        RefreshTagIndex();
        _catalogChangesEnabled = true;
        _diagnosticCatalog.Changed += DiagnosticCatalog_Changed;

        _diagnosticReportReceiver.ReportSent += DiagnosticReportReceiver_DiagnosticReportSent;
        _diagnosticReportReceiver.ReportsSent += DiagnosticReportReceiver_DiagnosticReportsSent;
        _elementChangesEnabled = true;
        _elementChangesReceiver.Sent += ElementChangesReceiver_ElementChangesSent;
        _ignoreListManager.EntriesRemoved += IgnoreListManager_EntriesRemoved;

        TargetDocumentTitle = _revitContext.ActiveDocument?.Title;
    }

    protected async override Task OnDeinitializing(CancellationToken cancellationToken = default)
    {
        _rowAgeTicker?.Dispose();
        _rowAgeTicker = null;
        await RestoreActiveVisualizationAsync(cancellationToken);
        await RestoreHeatmapScheduledAsync(cancellationToken);
        _catalogChangesEnabled = false;
        _diagnosticCatalog.Changed -= DiagnosticCatalog_Changed;
        _diagnosticReportReceiver.ReportSent -= DiagnosticReportReceiver_DiagnosticReportSent;
        _diagnosticReportReceiver.ReportsSent -= DiagnosticReportReceiver_DiagnosticReportsSent;
        _elementChangesEnabled = false;
        _elementChangesReceiver.Sent -= ElementChangesReceiver_ElementChangesSent;
        _ignoreListManager.EntriesRemoved -= IgnoreListManager_EntriesRemoved;
        _pendingElementRefreshes.Clear();
        _pendingDocumentRefreshes.Clear();
        StopDiagnosticRefreshTimer();
        _catalogLease?.Dispose();
        _catalogLease = null;
        _dispatcher = null;
        await base.OnDeinitializing(cancellationToken);
    }

    private void RefreshRowAges()
    {
        foreach (DiagnosticReportItemViewModel row in Collection)
            row.RefreshCreatedText();
    }

    private void DiagnosticCatalog_Changed(object? sender, DiagnosticCatalogChangedEventArgs args)
    {
        if (!_catalogChangesEnabled) return;
        Dispatcher? dispatcher = _dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted) return;
        if (!dispatcher.CheckAccess())
        {
            _ = dispatcher.InvokeAsync(() =>
            {
                if (_catalogChangesEnabled) ReplaceCatalogSnapshot(args.Origin);
            });
            return;
        }

        ReplaceCatalogSnapshot(args.Origin);
    }

    private void ReplaceCatalogSnapshot(DiagnosticCatalogChangeOrigin origin)
    {
        IDiagnosticCatalogSnapshotLease lease = _diagnosticCatalog.AcquireSnapshot();

        // A scope refresh only narrows the published checks. The findings already shown stay, because a
        // session that takes over the diagnostics list (the practical tour) must not erase the report it
        // is about to explain; its next run replaces the report anyway. Any other refresh may invalidate
        // the rows, so it clears them as before.
        if (origin is not DiagnosticCatalogChangeOrigin.Scope)
            Clear();

        IDiagnosticCatalogSnapshotLease? previous = _catalogLease;
        _catalogLease = lease;
        RefreshTagIndex();
        previous?.Dispose();
    }

    // Rebuilds the code-to-tags index from the current snapshot. A code that is no longer registered,
    // for example after its rule was removed from a configuration file, keeps no tags.
    private void RefreshTagIndex()
    {
        _tagsByCode.Clear();
        DiagnosticCatalogSnapshot? snapshot = _catalogLease?.Snapshot;
        if (snapshot is null) return;

        foreach (IDiagnosticIdentity identity in snapshot.ElementDiagnostics.Select(registration => registration.Identity))
            _tagsByCode[identity.Code] = identity.Tags;
        foreach (IDiagnosticIdentity identity in snapshot.DocumentDiagnostics.Select(registration => registration.Identity))
            _tagsByCode[identity.Code] = identity.Tags;
    }

    protected override void OnRevitChanged(RevitEventType revitEventType) {
        if (_visualizationMutationDepth > 0 &&
            revitEventType is RevitEventType.DocumentChanged or RevitEventType.ViewActivated)
        {
            _logger.LogDebug(
                "Ignoring {RevitEventType} raised by the active visualization operation",
                revitEventType);
        }
        else
        {
            if (_activeVisualizationPipeline is not null)
                _logger.LogInformation(
                    "Scheduling active visualization restoration after {RevitEventType}",
                    revitEventType);
            _ = RestoreActiveVisualizationAsync();
        }
        TargetDocumentTitle = _revitContext.ActiveDocument?.Title;

        // A heat map belongs to the document whose view it colours, so switching to another document or
        // closing that document drops it. Any other Revit event, including a model change, keeps the
        // snapshot as it is.
        if (revitEventType is RevitEventType.DocumentFocusChanged or RevitEventType.DocumentClosed)
            RestoreHeatmapIfDocumentChanged();
    }

    private void ElementChangesReceiver_ElementChangesSent(object? sender, ElementChangesSentEventArgs e)
    {
        // A paused report keeps the rows it shows, so a model change neither removes them nor schedules
        // a check of the changed elements. The change itself is not remembered: the pause only affects
        // updates that happen after it ends.
        if (!_documentChangesRefreshEnabled) return;

        var changes = e.Changes;

        Document? targetDocument = changes.Document;
        if (targetDocument is not { IsValidObject: true }) return;

        // The changed elements are checked themselves, not only the rows that mention them. A row
        // removed because its finding disappeared is not found by a later undo, and a created element
        // has no row that could name it, so both used to stay unchecked until a full run.
        HashSet<ElementId> toRefresh = [.. changes.Modified, .. changes.Creared];

        foreach (var id in changes.Modified)
        {
            var targets = GetTargetRows(targetDocument, id);
            foreach (var target in targets)
            {
                bool success = Collection.Remove(target);
                if (success && target.Target is Element { IsValidObject: true } element)
                    toRefresh.Add(element.Id);
            }
        }

        foreach (var id in changes.Deleted)
        {
            var targets = GetTargetRows(targetDocument, id);
            foreach (var target in targets) Collection.Remove(target);
        }

        foreach (var id in changes.Creared)
        {
            var targets = GetTargetRows(targetDocument, id);
            foreach (var target in targets)
            {
                if (target.Target is Element { IsValidObject: true } element)
                    toRefresh.Add(element.Id);
            }
        }

        // A non-committed operation can revert an ignore. The ignore transaction changes only the ignore
        // storage, not the target elements, so the changed identifiers never name the rows it removed.
        // They are remembered and dropped before the check, because a redo makes the ignore active again.
        if (changes.Operation != UndoOperation.TransactionCommitted
            && _ignoredElementIds.TryGetValue(targetDocument, out HashSet<ElementId>? ignoredElementIds)
            && ignoredElementIds.Count > 0)
        {
            foreach (ElementId ignoredElementId in ignoredElementIds)
            {
                foreach (DiagnosticReportItemViewModel row in GetTargetRows(targetDocument, ignoredElementId))
                    Collection.Remove(row);
            }

            toRefresh.UnionWith(ignoredElementIds);
        }

        QueueRefresh(targetDocument, toRefresh);
    }

    // Entries removed in the Ignoring pane are a committed change of the stored list, not of the model,
    // so the element change event never names their targets. The removed entries are checked again here;
    // the identifiers of earlier ignores cover the rows a document-level entry suppressed.
    private void IgnoreListManager_EntriesRemoved(object? sender, IgnoreListEntriesRemovedEventArgs e)
    {
        if (!_documentChangesRefreshEnabled) return;

        Document? targetDocument = e.Document;
        if (targetDocument is not { IsValidObject: true }) return;

        HashSet<ElementId> toRefresh = [.. e.ElementIds];
        if (_ignoredElementIds.TryGetValue(targetDocument, out HashSet<ElementId>? ignoredElementIds))
            toRefresh.UnionWith(ignoredElementIds);

        foreach (ElementId elementId in toRefresh)
        {
            foreach (DiagnosticReportItemViewModel row in GetTargetRows(targetDocument, elementId))
                Collection.Remove(row);
        }

        QueueRefresh(targetDocument, toRefresh);
    }

    private void QueueRefresh(Document document, HashSet<ElementId> elementIds)
    {
        // Any change can move a document-level finding, even a deletion that leaves no element to check.
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

        StartDiagnosticRefreshTimer();
    }

    private List<DiagnosticReportItemViewModel> GetTargetRows(Document document, ElementId id)
        => Collection
            .Where(i => i.DocumentTitle == document.Title
                && (Equals(i.TargetElementId, id) || i.TargetDependencyElementIds.Contains(id)))
            .ToList();

    private void StartDiagnosticRefreshTimer()
    {
        if (_diagnosticRefreshTimer is not null)
        {
            if (!_diagnosticRefreshTimer.IsEnabled) _diagnosticRefreshTimer.Start();
            return;
        }

        Dispatcher? dispatcher = _dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted) return;

        _diagnosticRefreshTimer = new DispatcherTimer(
            DiagnosticRefreshPollInterval,
            DispatcherPriority.Background,
            DiagnosticRefreshTimer_Tick,
            dispatcher);
        _diagnosticRefreshTimer.Start();
    }

    private void StopDiagnosticRefreshTimer() => _diagnosticRefreshTimer?.Stop();

    private void DiagnosticRefreshTimer_Tick(object? sender, EventArgs e)
    {
        if (!_elementChangesEnabled || !_documentChangesRefreshEnabled)
        {
            StopDiagnosticRefreshTimer();
            return;
        }

        if (_diagnosticRefreshScheduled) return;

        long now = System.Diagnostics.Stopwatch.GetTimestamp();
        bool elementRefreshDue = _pendingElementRefreshes.Count > 0 && IsElementRefreshDue(now);
        bool documentRefreshDue = _pendingDocumentRefreshes.Count > 0 && IsDocumentDiagnosticsRefreshDue(now);

        if (!elementRefreshDue && !documentRefreshDue)
        {
            if (_pendingElementRefreshes.Count == 0 && _pendingDocumentRefreshes.Count == 0)
                StopDiagnosticRefreshTimer();
            return;
        }

        _diagnosticRefreshScheduled = true;
        StopDiagnosticRefreshTimer();
        _ = _idlingScheduler.RunAsync(_ => FlushPendingDiagnosticRefreshes());
    }

    private bool IsElementRefreshDue(long now)
        => GetElapsed(_lastPendingChangeTimestamp, now) >= DiagnosticRefreshQuietPeriod
        || GetElapsed(_firstPendingChangeTimestamp, now) >= DiagnosticRefreshMaximumDelay;

    private bool IsDocumentDiagnosticsRefreshDue(long now)
        => _lastDocumentDiagnosticsTimestamp == 0
        || GetElapsed(_lastDocumentDiagnosticsTimestamp, now) >= DocumentDiagnosticsRefreshPeriod;

    private static TimeSpan GetElapsed(long from, long to)
        => TimeSpan.FromSeconds((to - from) / (double)System.Diagnostics.Stopwatch.Frequency);

    private void FlushPendingDiagnosticRefreshes()
    {
        try
        {
            if (!_elementChangesEnabled) return;
            if (!_documentChangesRefreshEnabled)
            {
                _pendingElementRefreshes.Clear();
                _pendingDocumentRefreshes.Clear();
                return;
            }

            long now = System.Diagnostics.Stopwatch.GetTimestamp();
            bool includeElementRefresh =
                _pendingElementRefreshes.Count > 0 && IsElementRefreshDue(now);
            bool includeDocumentDiagnostics =
                _pendingDocumentRefreshes.Count > 0 && IsDocumentDiagnosticsRefreshDue(now);

            if (includeElementRefresh)
            {
                List<KeyValuePair<Document, HashSet<ElementId>>> elementRefreshes = [.. _pendingElementRefreshes];
                _pendingElementRefreshes.Clear();

                foreach (KeyValuePair<Document, HashSet<ElementId>> entry in elementRefreshes)
                {
                    Document document = entry.Key;
                    if (document is not { IsValidObject: true } || entry.Value.Count == 0) continue;

                    // The document rows the previous run produced are replaced by this one. Without
                    // the removal every incremental run of a failing document diagnostic added a row.
                    if (includeDocumentDiagnostics) RemoveDocumentRows(document);
                    _diagnosticService.Execute(
                        document, entry.Value, includeDocumentDiagnostics: includeDocumentDiagnostics);
                    if (includeDocumentDiagnostics) _pendingDocumentRefreshes.Remove(document);
                }
            }

            if (!includeDocumentDiagnostics) return;

            _lastDocumentDiagnosticsTimestamp = now;
            List<Document> documentRefreshes = [.. _pendingDocumentRefreshes];
            _pendingDocumentRefreshes.Clear();
            foreach (Document document in documentRefreshes)
            {
                if (document is not { IsValidObject: true }) continue;

                RemoveDocumentRows(document);
                _diagnosticService.ExecuteDocumentDiagnostics(document);
            }
        }
        finally
        {
            _diagnosticRefreshScheduled = false;
            if (_pendingElementRefreshes.Count > 0 || _pendingDocumentRefreshes.Count > 0)
                StartDiagnosticRefreshTimer();
            else
                StopDiagnosticRefreshTimer();
        }
    }

    // Document diagnostics report against the document itself, so their rows are recognized by the
    // document target rather than an element identifier.
    private void RemoveDocumentRows(Document document)
    {
        string documentTitle = document.Title;
        List<DiagnosticReportItemViewModel> rows = Collection
            .Where(item => item.Target is Document
                && string.Equals(item.DocumentTitle, documentTitle, StringComparison.Ordinal))
            .ToList();
        if (rows.Count == 0) return;

        foreach (DiagnosticReportItemViewModel row in rows)
            Collection.Remove(row);

        _logger.LogDebug(
            "Removed {RowCount} document report rows of {DocumentTitle} before the refresh",
            rows.Count, documentTitle);
    }

    private void DiagnosticReportReceiver_DiagnosticReportSent(object? sender, DiagnosticMessageSentEventArgs e)
        => Collection.Add(CreateItem(e.Report, e.Report.Document.Title));

    // The rows of a batch are created first and added with one collection notification. The two parts are
    // timed apart and reported by Refresh, because only the second one is saved by batching.
    private void DiagnosticReportReceiver_DiagnosticReportsSent(object? sender, DiagnosticReportsSentEventArgs e)
    {
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        List<DiagnosticReportItemViewModel> items = new(e.Reports.Count);

        // The reports of a batch normally belong to one document, so its title is read from Revit once.
        Document? titledDocument = null;
        string documentTitle = string.Empty;
        foreach (DiagnosticReport report in e.Reports)
        {
            if (!ReferenceEquals(report.Document, titledDocument))
            {
                titledDocument = report.Document;
                documentTitle = report.Document.Title;
            }

            items.Add(CreateItem(report, documentTitle));
        }
        long created = System.Diagnostics.Stopwatch.GetTimestamp();

        if (Collection is BatchObservableCollection<DiagnosticReportItemViewModel> batchCollection)
            batchCollection.AddRange(items);
        else
            foreach (DiagnosticReportItemViewModel item in items)
                Collection.Add(item);

        _batchRowCount += items.Count;
        _batchCreationTicks += created - started;
        _batchAdditionTicks += System.Diagnostics.Stopwatch.GetTimestamp() - created;
    }

    private int _batchRowCount;
    private long _batchCreationTicks;
    private long _batchAdditionTicks;

    private DiagnosticReportItemViewModel CreateItem(DiagnosticReport report, string documentTitle)
    {
        // Reports can outlive their targets: an element deleted or undone after the diagnostic ran
        // leaves a stale reference whose Id getter throws InvalidObjectException, so ids are read
        // defensively. A row with an unknown id stays visible, just without fixes or visualization.
        DiagnosticReportItemViewModel item = new() {
            Created = report.Created,
            ShowElementToolTipFormat = ShowElementToolTip,
            ExpandListToolTip = ExpandListToolTip,
            Code = report.Code,
            Template = report.Message.Format,
            Target = report.Target,
            TargetDependencies = report.TargetDependencies,
            TargetElementId = (report.Target as Element) is { } targetElement
                ? TryGetElementId(targetElement)
                : null,
            TargetDependencyElementIds = report.TargetDependencies?
                .OfType<Element>()
                .Select(TryGetElementId)
                .OfType<ElementId>()
                .ToArray() ?? [],
            FixesFactory = () => CreateFixes(report),
            VisualizationPipelinesFactory = () => CreateVisualizationPipelines(report),
            OpenDocumentationDelegate = () => OpenDocumentation(report.Code),
            Args = report.Message.Args.ToDictionary(i => i.Item1, i => i.Item2),
            Severity = report.Severity,
            DocumentTitle = documentTitle,
            AccentElementDelegate = i => SelectElement(i),
            IsObsolete = report.IsObsolete,
            ObsoleteDescription = report.ObsoleteDescription,
            Tags = GetTags(report.Code),
        };
        return item;
    }

    // Reads the id of an element that may already be deleted or undone. The Id getter of a stale
    // element throws InvalidObjectException instead of returning a value, even when IsValidObject
    // was true a moment earlier: rows are built lazily on the UI thread while the model can change
    // on the Revit thread in between.
    private static ElementId? TryGetElementId(Element element)
    {
        try
        {
            if (!element.IsValidObject) return null;
            return element.Id;
        }
        catch (Autodesk.Revit.Exceptions.InvalidObjectException)
        {
            return null;
        }
    }


    private static readonly DocumentationPage DiagnosticsOverviewPage = new("Diagnostics overview", "Обзор диагностики");

    /// <summary>
    /// Opens the documentation page of the module that registered the diagnostic in the default browser.
    /// </summary>
    /// <remarks>
    /// The page is resolved when the user asks for it, not when the row is created. A code that is no longer
    /// in the catalog, for example after its rule was removed from a configuration file, opens the
    /// diagnostics overview instead.
    /// </remarks>
    private void OpenDocumentation(string code) => _documentationLauncher.Open(
        _catalogLease?.Snapshot.FindDocumentation(code) ?? DiagnosticsOverviewPage);

    private static readonly DocumentationPage PanePage = new("Diagnostic reports pane", "Панель результатов диагностики");

    /// <summary>Opens the documentation of this pane; bound to F1 over the pane's help tooltips.</summary>
    [RelayCommand]
    private void OpenHelp() => _documentationLauncher.Open(PanePage);


    private DiagnosticCatalogSnapshot GetCatalogSnapshot() =>
        _catalogLease?.Snapshot ?? throw new InvalidOperationException("The diagnostic catalog is not initialized.");

    private IReadOnlyList<string> GetTags(string code) =>
        _tagsByCode.TryGetValue(code, out IReadOnlyList<string>? tags) ? tags : DiagnosticTags.None;
}
