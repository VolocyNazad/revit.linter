using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Revit.Context.Abstractions.Services;
using Revit.Linter.Diagnostic.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.DiagnosticReportPresenter.Interactions.Abstractions.Services;
using Revit.Linter.DiagnosticReportPresenter.Infrastructure;
using Revit.Linter.OpenedDocuments.ViewModels;
using Revit.Linter.DiagnosticReportExporting.Abstractions.Services;
using Revit.Linter.Presentation.ViewModels;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Services;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementChangesProvider.Abstractions.Models;
using Revit.Linter.ElementChangesProvider.Abstractions.Services;
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
        Collection.Clear();
        ClearFilters();
    }

    public void Clear(string documentTitle)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        RestoreActiveVisualizationOrSchedule();
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
    private readonly IDialog _dialog;
    private readonly IDocumentationLauncher _documentationLauncher;
    private readonly IUserInterfaceActivityStream _activityStream;
    private readonly IVisualizationViewActivator _visualizationViewActivator;
    private readonly ILogger<DiagnosticReportViewModel> _logger;
    private readonly IConfirmationDialog _confirmationDialog;
    private readonly IDiagnosticReportExportService _exportService;
    private FilteredRowCollection<DiagnosticReportItemViewModel>? _rows;
    private IDiagnosticCatalogSnapshotLease? _catalogLease;
    // Tags are looked up per report row; a code-keyed index keeps row creation O(1) on large reports.
    private readonly Dictionary<string, IReadOnlyList<string>> _tagsByCode = new(StringComparer.Ordinal);
    private IElementVisualizationPipeline? _activeVisualizationPipeline;
    private ElementId? _activeVisualizationTargetId;
    private int _visualizationMutationDepth;
    private bool _catalogChangesEnabled;
    private Dispatcher? _dispatcher;
    private readonly object _restoreScheduleSync = new();
    private Task? _scheduledRestore;

    private bool _elementChangesEnabled;
    private bool _elementRefreshScheduled;
    private readonly Dictionary<Document, HashSet<ElementId>> _pendingElementRefreshes = [];

    public DiagnosticReportViewModel(
            IRevitContext revitContext, IRevitIdlingScheduler idlingScheduler,
            IEnumerable<IAccentElementsService> accentElementsServices,
            IFixReportSender fixReportSender,
            IDiagnosticReportReceiver diagnosticReportReceiver, IElementChangesReceiver elementChangesReceiver,
            IDiagnosticCatalog diagnosticCatalog,
            IDiagnosticService diagnosticService, IIgnoreElementProvider ignoreElementProvider,
            IDialog dialog, IConfirmationDialog confirmationDialog,
            IDocumentationLauncher documentationLauncher,
            IUserInterfaceActivityStream activityStream,
            IVisualizationViewActivator visualizationViewActivator,
            IDiagnosticReportExportService exportService,
            OpenedDocumentsViewModel openedDocumentsViewModel,
            ILogger<DiagnosticReportViewModel> logger) : base(idlingScheduler)
    {
        _accentElementsServices = accentElementsServices;
        _diagnosticReportReceiver = diagnosticReportReceiver;
        _elementChangesReceiver = elementChangesReceiver;
        _revitContext = revitContext;
        _fixReportSender = fixReportSender;
        _diagnosticCatalog = diagnosticCatalog;
        _diagnosticService = diagnosticService;
        _ignoreElementProvider = ignoreElementProvider;
        _dialog = dialog;
        _documentationLauncher = documentationLauncher;
        _activityStream = activityStream;
        _visualizationViewActivator = visualizationViewActivator;
        _logger = logger;
        _confirmationDialog = confirmationDialog;
        _exportService = exportService;
        OpenedDocumentsViewModel = openedDocumentsViewModel;

        Collection = new BatchObservableCollection<DiagnosticReportItemViewModel>();
    }

    /// <summary>
    /// Gets the opened-document selector displayed by the view.
    /// </summary>
    public OpenedDocumentsViewModel OpenedDocumentsViewModel { get; }

    [ObservableProperty]
    public partial ObservableCollection<DiagnosticReportItemViewModel> Collection { get; private set; }
    partial void OnCollectionChanged(ObservableCollection<DiagnosticReportItemViewModel> value)
    {
        Rows.Attach(value);
        CollectionViewSource = Rows.ViewSource;
    }

    [ObservableProperty]
    public partial CollectionViewSource? CollectionViewSource { get; private set; }

    [ObservableProperty]
    public partial string SearchField { get; set; } = string.Empty;
    partial void OnSearchFieldChanged(string value) => Rows.Refresh();

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
            [new SortDescription(nameof(DiagnosticReportItemViewModel.Code), ListSortDirection.Ascending)],
            IsRowVisible);

    private bool IsRowVisible(DiagnosticReportItemViewModel viewModel)
        => (string.IsNullOrEmpty(TargetDocumentTitle) || string.Equals(TargetDocumentTitle, viewModel.DocumentTitle))
        && SeverityFilters.Any(filter => filter.IsActive && filter.IsValid(viewModel))
        && Filters.Any(filter => filter.IsActive && filter.IsValid(viewModel))
        && (viewModel.MessageText.Contains(SearchField, StringComparison.CurrentCultureIgnoreCase)
        || viewModel.Code.Contains(SearchField, StringComparison.CurrentCultureIgnoreCase)
        || DiagnosticTags.Matches(viewModel.Tags, SearchField));

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

        TargetDocumentTitle = _revitContext.ActiveDocument?.Title;
    }

    protected async override Task OnDeinitializing(CancellationToken cancellationToken = default)
    {
        await RestoreActiveVisualizationAsync(cancellationToken);
        _catalogChangesEnabled = false;
        _diagnosticCatalog.Changed -= DiagnosticCatalog_Changed;
        _diagnosticReportReceiver.ReportSent -= DiagnosticReportReceiver_DiagnosticReportSent;
        _diagnosticReportReceiver.ReportsSent -= DiagnosticReportReceiver_DiagnosticReportsSent;
        _elementChangesEnabled = false;
        _elementChangesReceiver.Sent -= ElementChangesReceiver_ElementChangesSent;
        _pendingElementRefreshes.Clear();
        _catalogLease?.Dispose();
        _catalogLease = null;
        _dispatcher = null;
        await base.OnDeinitializing(cancellationToken);
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
                if (_catalogChangesEnabled) ReplaceCatalogSnapshot();
            });
            return;
        }

        ReplaceCatalogSnapshot();
    }

    private void ReplaceCatalogSnapshot()
    {
        IDiagnosticCatalogSnapshotLease lease = _diagnosticCatalog.AcquireSnapshot();
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
    }

    private void ElementChangesReceiver_ElementChangesSent(object? sender, ElementChangesSentEventArgs e)
    {
        var changes = e.Changes;

        Document? targetDocument = changes.Document;
        if (targetDocument is not { IsValidObject: true }) return;

        HashSet<ElementId> toRefresh = [];

        foreach (var id in changes.Modified)
        {
            var targets = GetTargetItemsBy(id);
            foreach (var target in targets)
            {
                bool success = Collection.Remove(target);
                if (success && target.Target is Element { IsValidObject: true } element)
                    toRefresh.Add(element.Id);
            }
        }

        foreach (var id in changes.Deleted)
        {
            var targets = GetTargetItemsBy(id);
            foreach (var target in targets) Collection.Remove(target);
        }

        foreach (var id in changes.Creared)
        {
            var targets = GetTargetItemsBy(id);
            foreach (var target in targets)
            {
                if (target.Target is Element { IsValidObject: true } element)
                    toRefresh.Add(element.Id);
            }
        }

        if (toRefresh.Count > 0)
        {
            if (!_pendingElementRefreshes.TryGetValue(targetDocument, out HashSet<ElementId>? pending))
            {
                pending = [];
                _pendingElementRefreshes[targetDocument] = pending;
            }
            pending.UnionWith(toRefresh);

            ScheduleElementDiagnosticsRefresh();
        }

        List<DiagnosticReportItemViewModel> GetTargetItemsBy(ElementId id) {
            return Collection
                .Where(i => i.DocumentTitle == targetDocument.Title
                && (Equals(i.TargetElementId, id) || i.TargetDependencyElementIds.Contains(id)))
                .ToList();
        }
    }

    private void ScheduleElementDiagnosticsRefresh()
    {
        if (_elementRefreshScheduled) return;
        _elementRefreshScheduled = true;

        _ = _idlingScheduler.RunAsync(_ => FlushPendingElementDiagnosticsRefreshes());
    }

    private void FlushPendingElementDiagnosticsRefreshes()
    {
        _elementRefreshScheduled = false;

        if (!_elementChangesEnabled || _pendingElementRefreshes.Count == 0) return;

        List<KeyValuePair<Document, HashSet<ElementId>>> pending = [.. _pendingElementRefreshes];
        _pendingElementRefreshes.Clear();

        foreach (KeyValuePair<Document, HashSet<ElementId>> entry in pending)
        {
            Document document = entry.Key;
            HashSet<ElementId> elementIds = entry.Value;

            if (document is not { IsValidObject: true } || elementIds.Count == 0) continue;

            _diagnosticService.Execute(document, elementIds);
        }
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
