using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Revit.Linter.ThemeManaging.Controls;
using Microsoft.Extensions.Logging;
using Revit.Async;
using Revit.Context.Abstractions.Services;
using Revit.Linter.Diagnostic.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.DiagnosticReportPresenter.Interactions.Abstractions.Services;
using Revit.Linter.DiagnosticReportPresenter.Exporting;
using Revit.Linter.DiagnosticReportPresenter.Infrastructure;
using Revit.Linter.Presentation.ViewModels;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Services;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementChangesProvider.Abstractions.Models;
using Revit.Linter.ElementChangesProvider.Abstractions.Services;
using Revit.Linter.ElementIgnoring.Abstractions.Models;
using Revit.Linter.ElementIgnoring.Abstractions.Services;
using Revit.Linter.FixReportProvider.Abstractions.Models;
using Revit.Linter.FixReportProvider.Abstractions.Services;
using Revit.Linter.Localization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels;

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


    public void Refresh()
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
    private readonly ILogger<DiagnosticReportViewModel> _logger;
    private readonly IConfirmationDialog _confirmationDialog;
    private readonly IReadOnlyList<IDiagnosticReportExporter> _reportExporters;
    private IDiagnosticCatalogSnapshotLease? _catalogLease;
    private IElementVisualizationPipeline? _activeVisualizationPipeline;
    private ElementId? _activeVisualizationTargetId;
    private int _visualizationMutationDepth;
    private bool _catalogChangesEnabled;
    private Dispatcher? _dispatcher;

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
            IEnumerable<IDiagnosticReportExporter> reportExporters,
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
        _logger = logger;
        _confirmationDialog = confirmationDialog;
        _reportExporters = reportExporters.ToArray();

        Collection = new BatchObservableCollection<DiagnosticReportItemViewModel>();
    }

    [ObservableProperty]
    public partial ObservableCollection<DiagnosticReportItemViewModel> Collection { get; private set; }
    partial void OnCollectionChanged(ObservableCollection<DiagnosticReportItemViewModel> value)
        => InitializeCollectionView();

    [ObservableProperty]
    public partial CollectionViewSource? CollectionViewSource { get; private set; }

    [ObservableProperty]
    public partial string SearchField { get; set; } = string.Empty;
    partial void OnSearchFieldChanged(string value) => RefreshCollectionView();

    [ObservableProperty]
    public partial IEnumerable<IDiagnosticReportFilter> SeverityFilters { get; set; } = [];
    partial void OnSeverityFiltersChanged(
        IEnumerable<IDiagnosticReportFilter> oldValue, IEnumerable<IDiagnosticReportFilter> newValue)
        => UpdateFilterSubscriptions(oldValue, newValue);

    [ObservableProperty]
    public partial IEnumerable<IDiagnosticReportFilter> Filters { get; set; } = [];
    partial void OnFiltersChanged(
        IEnumerable<IDiagnosticReportFilter> oldValue, IEnumerable<IDiagnosticReportFilter> newValue)
        => UpdateFilterSubscriptions(oldValue, newValue);

    private void UpdateFilterSubscriptions(
        IEnumerable<IDiagnosticReportFilter> oldValue, IEnumerable<IDiagnosticReportFilter> newValue)
    {
        if (oldValue != null)
            foreach (var filter in oldValue)
                filter.PropertyChanged -= Filter_PropertyChanged;
        if (newValue != null)
            foreach (var filter in newValue)
                filter.PropertyChanged += Filter_PropertyChanged;
        RefreshCollectionView();
    }

    [ObservableProperty]
    public partial string? TargetDocumentTitle { get; set; }
    partial void OnTargetDocumentTitleChanged(string? value) => RefreshCollectionView();

    #region [ShowElement] Command - Show element

    /// <summary> Show element </summary>
    [RelayCommand(CanExecute = nameof(CanShowElement))]
    private void ShowElement(object? parameter)
    {
#if BEFORE2024
        if (parameter is not int elementId) return;
#else
        if (parameter is not long elementId) return;
#endif

        ShowElement(new(elementId));
    }
    private void ShowElement(ElementId elementId)
    {
        Document? targetdocument = _revitContext.ActiveDocument;
        if (targetdocument is null) return;

        _accentElementsServices
            .First(i => i.Type == AccentElementsType.ShowElements)
            .Execute(targetdocument, elementId);
    }
    private bool CanShowElement(object? elementId)
#if BEFORE2024
        => elementId is int
#else
        => elementId is long
#endif
        && _revitContext.ActiveDocument is { IsFamilyDocument: false };

    #endregion

    #region [SelectElement] Command - Select element

    /// <summary> Select element </summary>
    [RelayCommand(CanExecute = nameof(CanSelectElement))]
    private void SelectElement(object? parameter)
    {
        if (parameter is ElementId element)
        {
            SelectElement(element);
            return;
        }
#if BEFORE2024
        if (parameter is not int elementId) return;
#else
        if (parameter is not long elementId) return;
#endif

        SelectElement(new(elementId));
    }
    private void SelectElement(ElementId elementId)
    {
        Document? targetdocument = _revitContext.ActiveDocument;
        if (targetdocument is null) return;

        _accentElementsServices
            .First(i => i.Type == AccentElementsType.SelectElements)
            .Execute(targetdocument, elementId);
    }
    private bool CanSelectElement(object? elementId)
#if BEFORE2024
        => elementId is int
#else
        => elementId is long or ElementId
#endif
        && _revitContext.ActiveDocument is { IsFamilyDocument: false };

    #endregion

    #region [IsolateElementsOnView] Command - Isolate element in the active view

    /// <summary> Isolate element in the active view </summary>
    [RelayCommand(CanExecute = nameof(CanIsolateElementsOnView))]
    private void IsolateElementsOnView(object? parameter)
    {
#if BEFORE2024
        if (parameter is not int elementId) return;
#else
        if (parameter is not long elementId) return;
#endif

        Document? targetdocument = _revitContext.ActiveDocument;
        if (targetdocument is null) return;

        if (targetdocument.ActiveView is not null)

            _accentElementsServices
                .First(i => i.Type == AccentElementsType.IsolateElementsOnView)
                .Execute(targetdocument, new ElementId(elementId));
    }
    private bool CanIsolateElementsOnView(object? elementId)
#if BEFORE2024
        => elementId is int
#else
        => elementId is long
#endif
        && _revitContext.ActiveDocument is { IsFamilyDocument: false }
        && _revitContext.ActiveDocument.ActiveView is not null;

    #endregion

    #region [CutViewByElement] Command - Crop the active 3D view by element

    /// <summary> Crop the active 3D view by element </summary>
    [RelayCommand(CanExecute = nameof(CanCutViewByElement))]
    private void CutViewByElement(object? parameter)
    {
#if BEFORE2024
        if (parameter is not int elementId) return;
#else
        if (parameter is not long elementId) return;
#endif

        Document? targetdocument = _revitContext.ActiveDocument;
        if (targetdocument is null) return;

        if (targetdocument.ActiveView is View3D) return;

        _accentElementsServices
            .First(i => i.Type == AccentElementsType.CutViewByElements)
            .Execute(targetdocument, new ElementId(elementId));
    }
    private bool CanCutViewByElement(object? elementId)
#if BEFORE2024
        => elementId is int
#else
        => elementId is long
#endif
        && _revitContext.ActiveDocument is { IsFamilyDocument: false }
        && _revitContext.ActiveDocument.ActiveView is View3D;

    #endregion

    #region [CopyToClipboard] Command - Copy text to clipboard

    /// <summary> Copy text to clipboard </summary>
    [RelayCommand]
    private void CopyToClipboard(object item) => Clipboard.SetText(item.ToString() ?? string.Empty);

    #endregion

    #region [Export] Command - Export

    /// <summary> Export </summary>
    [RelayCommand]
    private void Export()
    {
        if (_reportExporters.Count == 0) return;

        SaveFileDialog dialog = new()
        {
            AddExtension = true,
            DefaultExt = _reportExporters[0].Extension,
            FileName = CreateExportFileName(),
            Filter = string.Join("|", _reportExporters.Select(exporter => exporter.Filter)),
            FilterIndex = 1,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true) return;

        List<DiagnosticReportExportItem> items = (CollectionViewSource?.View
            .Cast<object>()
            .OfType<DiagnosticReportItemViewModel>() ?? [])
            .Select(item => new DiagnosticReportExportItem(
                item.Severity.ToString(),
                item.SeverityText,
                item.Code,
                item.MessageText,
                item.DocumentTitle,
                new DateTimeOffset(item.Created),
                item.IsObsolete,
                string.IsNullOrWhiteSpace(item.ObsoleteDescription) ? null : item.ObsoleteDescription,
                item.TargetElementId?.ToString(),
                item.TargetDependencyElementIds
                    .Select(elementId => elementId.ToString())
                    .ToArray()))
            .ToList();

        int exporterIndex = dialog.FilterIndex - 1;
        IDiagnosticReportExporter exporter = exporterIndex >= 0 && exporterIndex < _reportExporters.Count
            ? _reportExporters[exporterIndex]
            : _reportExporters[0];
        string fileName = Path.ChangeExtension(dialog.FileName, exporter.Extension);
        string documentTitle = string.IsNullOrWhiteSpace(TargetDocumentTitle)
            ? HtmlAllDocumentsText
            : TargetDocumentTitle ?? HtmlAllDocumentsText;
        Assembly assembly = typeof(DiagnosticReportViewModel).Assembly;
        DiagnosticReportExportDocument document = new(
            "1.0",
            new DiagnosticReportExportMetadata(
                DateTimeOffset.Now,
                _revitContext.ControlledApplication?.VersionNumber ?? string.Empty,
                _revitContext.ControlledApplication?.VersionBuild ?? string.Empty,
                assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                    ?? assembly.GetName().Version?.ToString()
                    ?? string.Empty,
                documentTitle,
                string.IsNullOrWhiteSpace(TargetDocumentTitle) ? "AllDocuments" : "Document",
                new DiagnosticReportExportFilters(
                    string.IsNullOrWhiteSpace(SearchField) ? null : SearchField,
                    SeverityFilters
                        .OfType<DiagnosticSeverityFilterViewModel>()
                        .Where(filter => filter.IsActive)
                        .Select(filter => filter.Value.ToString())
                        .ToArray(),
                    Filters
                        .Where(filter => filter.IsActive)
                        .Select(filter => filter is DiagnosticReportObsoleteFilterViewModel
                            ? "Obsolete"
                            : "Actual")
                        .ToArray()),
                Collection.Count,
                items.Count),
            items);
        DiagnosticReportExportContext context = new()
        {
            Culture = CultureInfo.CurrentCulture,
            UiCulture = CultureInfo.CurrentUICulture,
            SeverityHeader = SeverityHeader,
            CodeHeader = CodeHeader,
            MessageHeader = MessageHeader,
            DocumentHeader = DocumentHeader,
            CreatedHeader = CreatedHeader,
            ReportTitle = HtmlReportTitle,
            GeneratedLabel = HtmlGeneratedLabel,
            TotalLabel = HtmlTotalLabel,
            SummaryByCodeTitle = HtmlSummaryByCodeTitle,
            CountHeader = HtmlCountHeader,
            DetailsTitle = HtmlDetailsTitle,
            NoResultsText = HtmlNoResultsText,
            ErrorText = DiagnosticSeverityLocalizations.GetString(DiagnosticSeverity.Error.ToString()),
            WarningText = DiagnosticSeverityLocalizations.GetString(DiagnosticSeverity.Warning.ToString()),
            MessageText = DiagnosticSeverityLocalizations.GetString(DiagnosticSeverity.Message.ToString())
        };
        exporter.Export(fileName, context, document);
    }

    private string CreateExportFileName()
    {
        string document = string.IsNullOrWhiteSpace(TargetDocumentTitle)
            ? "DiagnosticReport"
            : TargetDocumentTitle ?? "DiagnosticReport";
        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        string safeDocument = string.Concat(document.Select(character =>
            invalidCharacters.Contains(character) ? '_' : character));
        return $"{safeDocument}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
    }

    #endregion

    private void Filter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        => RefreshCollectionView();

    private void InitializeCollectionView()
    {
        CollectionViewSource = new()
        {
            Source = Collection
        };

        CollectionViewSource.Filter += CollectionViewSource_Filter;

        CollectionViewSource.SortDescriptions.Clear();
        CollectionViewSource.SortDescriptions.Add(
            new SortDescription(nameof(DiagnosticReportItemViewModel.Code), ListSortDirection.Ascending));
    }

    private void RefreshCollectionView() => CollectionViewSource?.View.Refresh();

    private void CollectionViewSource_Filter(object sender, FilterEventArgs args)
    {
        args.Accepted = args.Item is DiagnosticReportItemViewModel viewModel
            && (string.IsNullOrEmpty(TargetDocumentTitle) || string.Equals(TargetDocumentTitle, viewModel.DocumentTitle))
            && SeverityFilters.Where(i => i.IsActive).Any(filter => filter.IsValid(viewModel))
            && Filters.Where(i => i.IsActive).Any(filter => filter.IsValid(viewModel))
            && (viewModel.MessageText.Contains(SearchField, StringComparison.CurrentCultureIgnoreCase)
            || viewModel.Code.Contains(SearchField, StringComparison.CurrentCultureIgnoreCase));
    }

    private void ClearFilters()
    {
        SeverityFilters = [];
        Filters = [];
    }

    private void RefreshFilters()
    {
        List<IDiagnosticReportFilter> severityFilters = [];

        severityFilters.Add(
            new DiagnosticSeverityFilterViewModel(true, DiagnosticSeverity.Message, Collection.Count(i => i.Severity == DiagnosticSeverity.Message)));
        severityFilters.Add(
            new DiagnosticSeverityFilterViewModel(true, DiagnosticSeverity.Warning, Collection.Count(i => i.Severity == DiagnosticSeverity.Warning)));
        severityFilters.Add(
            new DiagnosticSeverityFilterViewModel(true, DiagnosticSeverity.Error, Collection.Count(i => i.Severity == DiagnosticSeverity.Error)));

        SeverityFilters = severityFilters;

        List<IDiagnosticReportFilter> filters = [];

        filters.Add(new DiagnosticReportObsoleteFilterViewModel(true, Collection.Count(i => i.IsObsolete)));
        filters.Add(new DiagnosticReportActualFilterViewModel(true, Collection.Count(i => !i.IsObsolete)));

        Filters = filters;
    }

    protected async override Task OnInitializing(CancellationToken cancellationToken = default)
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        await base.OnInitializing(cancellationToken);
        IDiagnosticCatalogSnapshotLease lease = _diagnosticCatalog.AcquireSnapshot();
        _catalogLease?.Dispose();
        _catalogLease = lease;
        _catalogChangesEnabled = true;
        _diagnosticCatalog.Changed += DiagnosticCatalog_Changed;

        _diagnosticReportReceiver.ReportSent += DiagnosticReportReceiver_DiagnosticReportSent;
        _diagnosticReportReceiver.ReportsSent += DiagnosticReportReceiver_DiagnosticReportsSent;
        _elementChangesEnabled = true;
        _elementChangesReceiver.Sent += ElementChangesReceiver_ElementChangesSent;

        TargetDocumentTitle = _revitContext.ActiveDocument?.Title;

        ShowElementCommand.NotifyCanExecuteChanged();
        SelectElementCommand.NotifyCanExecuteChanged();
        IsolateElementsOnViewCommand.NotifyCanExecuteChanged();
        CutViewByElementCommand.NotifyCanExecuteChanged();
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
        previous?.Dispose();
    }

    protected override void OnRevitChanged(RevitEventType revitEventType) {
        if (revitEventType == RevitEventType.DocumentChanged && _visualizationMutationDepth > 0)
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

        ShowElementCommand.NotifyCanExecuteChanged();
        SelectElementCommand.NotifyCanExecuteChanged();
        IsolateElementsOnViewCommand.NotifyCanExecuteChanged();
        CutViewByElementCommand.NotifyCanExecuteChanged();
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
        DiagnosticReportItemViewModel item = new() {
            Created = report.Created,
            ShowElementToolTipFormat = ShowElementToolTip,
            Code = report.Code,
            Template = report.Message.Format,
            Target = report.Target,
            TargetDependencies = report.TargetDependencies,
            TargetElementId = (report.Target as Element)?.Id,
            TargetDependencyElementIds = report.TargetDependencies?
                .OfType<Element>()
                .Select(element => element.Id)
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
        };
        return item;
    }

    private IReadOnlyList<VisualizationPipelineViewModel> CreateVisualizationPipelines(DiagnosticReport report)
    {
        if (report.Target is not Element target)
        {
            _logger.LogWarning(
                "Visualization is unavailable for diagnostic {DiagnosticCode}: report target is not a Revit element",
                report.Code);
            return [];
        }

        ElementDiagnosticRegistration? registration = GetCatalogSnapshot().ElementDiagnostics
            .SingleOrDefault(item => string.Equals(
                item.Identity.Code, report.Code, StringComparison.Ordinal));
        if (registration is null)
        {
            _logger.LogWarning(
                "Visualization is unavailable for diagnostic {DiagnosticCode}: registration was not found",
                report.Code);
            return [];
        }

        _logger.LogDebug(
            "Created {VisualizationCount} visualization pipelines for diagnostic {DiagnosticCode}",
            registration.VisualizationPipelines.Count, report.Code);

        Element[] dependencies = report.TargetDependencies?.OfType<Element>().ToArray() ?? [];
        return registration.VisualizationPipelines.Select(pipeline => new VisualizationPipelineViewModel
        {
            Title = pipeline.Value,
            ShowDelegate = async cancellationToken =>
            {
                _preferredVisualizationName = pipeline.Value;
                _logger.LogInformation(
                    "Visualization requested: {VisualizationName}, diagnostic {DiagnosticCode}, target {TargetId}",
                    pipeline.Value, report.Code, target.Id);
                Document document = report.Document;
                if (document is not { IsValidObject: true })
                {
                    _logger.LogWarning(
                        "Visualization {VisualizationName} skipped for diagnostic {DiagnosticCode}: " +
                        "report document is no longer valid",
                        pipeline.Value, report.Code);
                    return;
                }
                if (target is not { IsValidObject: true })
                {
                    _logger.LogWarning(
                        "Visualization {VisualizationName} skipped for diagnostic {DiagnosticCode}: " +
                        "target element is no longer valid",
                        pipeline.Value, report.Code);
                    return;
                }
                Document? activeDocument = _revitContext.ActiveDocument;
                if (activeDocument is null || !activeDocument.Equals(document))
                {
                    _logger.LogWarning(
                        "Visualization {VisualizationName} skipped for diagnostic {DiagnosticCode}: " +
                        "report document is not active",
                        pipeline.Value, report.Code);
                    return;
                }

                try
                {
                    (bool success, bool restoredOnly) = await RevitTask.RunAsync(_ =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Document? currentDocument = _revitContext.ActiveDocument;
                        if (currentDocument is null || !currentDocument.Equals(document))
                            throw new InvalidOperationException("The report document is no longer active.");
                        if (!target.IsValidObject)
                            throw new InvalidOperationException("The visualization target is no longer valid.");

                        if (ReferenceEquals(_activeVisualizationPipeline, pipeline)
                            && _activeVisualizationTargetId?.Equals(target.Id) == true)
                        {
                            _logger.LogInformation(
                                "Visualization {VisualizationName} for diagnostic {DiagnosticCode} is already active; " +
                                "restoring it",
                                pipeline.Value, report.Code);
                            RestoreActiveVisualization();
                            return (true, true);
                        }

                        RestoreActiveVisualization();
                        ElementId[] dependencyIds = dependencies
                            .Where(element => element.IsValidObject)
                            .Select(element => element.Id)
                            .ToArray();
                        var elementSets = new Dictionary<string, IReadOnlyCollection<ElementId>>
                        {
                            [ElementVisualizationSetKeys.Target] = [target.Id],
                            [ElementVisualizationSetKeys.Dependencies] = dependencyIds
                        };
                        bool applied = ExecuteVisualizationMutation(() =>
                            pipeline.Apply(new(document, document.ActiveView, elementSets)));
                        if (applied)
                        {
                            _activeVisualizationPipeline = pipeline;
                            _activeVisualizationTargetId = target.Id;
                        }
                        return (applied, false);
                    });

                    if (!success && !restoredOnly)
                        await _dialog.Show(new DialogRequest(VisualizationFailedMessage), cancellationToken);
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Visualization {VisualizationName} failed for diagnostic {DiagnosticCode}, target {TargetId}",
                        pipeline.Value, report.Code, target.Id);
                    throw new InvalidOperationException(
                        $"Visualization '{pipeline.Value}' failed for diagnostic '{report.Code}'.",
                        exception);
                }
            }
        }).ToArray();
    }

    private void RestoreActiveVisualization()
    {
        IElementVisualizationPipeline? pipeline = _activeVisualizationPipeline;
        if (pipeline is not null)
            _logger.LogInformation(
                "Restoring active visualization {VisualizationName} for diagnostic {DiagnosticCode}",
                pipeline.Value, pipeline.Identity.Code);
        if (pipeline is not null)
            ExecuteVisualizationMutation(pipeline.Restore);
        _activeVisualizationPipeline = null;
        _activeVisualizationTargetId = null;
    }

    private T ExecuteVisualizationMutation<T>(Func<T> action)
    {
        _visualizationMutationDepth++;
        try
        {
            return action();
        }
        finally
        {
            _visualizationMutationDepth--;
        }
    }

    private void ExecuteVisualizationMutation(Action action) =>
        ExecuteVisualizationMutation(() =>
        {
            action();
            return true;
        });

    // Restoring a visualization starts a transaction, which Revit allows only in an API context, while the
    // report can be cleared from a WPF handler or a configuration-change notification. A failed restore
    // must not abort the clearing, so it is retried on the next Idling event; the pipeline keeps the
    // sessions it could not restore and has already logged the failure.
    private void RestoreActiveVisualizationOrSchedule()
    {
        if (_activeVisualizationPipeline is null) return;

        try
        {
            RestoreActiveVisualization();
        }
        catch (Exception)
        {
            _ = RestoreActiveVisualizationAsync();
        }
    }

    private async Task RestoreActiveVisualizationAsync(CancellationToken cancellationToken = default)
    {
        if (_activeVisualizationPipeline is null) return;

        try
        {
            await _idlingScheduler.RunAsync(_ => RestoreActiveVisualization(), cancellationToken);
        }
        catch (Exception)
        {
            // The pipeline logs the failure and retains sessions that still need restoration.
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

    private List<FixViewModel> CreateFixes(DiagnosticReport report)
    {
        if (report.Target is Element element)
        {
            var doc = report.Document;

            var elementId = element.Id;

            var iconColor = System.Windows.Media.Color.FromRgb(0xFF, 0xB7, 0x4D);
            PathIcon icon = new()
            {
                Kind = IconKind.Lightbulb,
                Foreground = new SolidColorBrush(iconColor)
            };
            FixViewModel ignoreFix = new()
            {
                Icon = icon,
                Title = IgnoreFixTitle,
                FixDelegate = async (cancellationToken) =>
                {
                    if (doc is null or { IsValidObject: false }) return;

                    if (!element.IsValidObject) return;

                    string? feedbackMessage = null;
                    string transactionName = GetLocalizedString("ignoreElement_transaction");
                    bool success = await ExecuteTransaction(doc, transactionName, () => {
                            var feedback = _ignoreElementProvider.Ignore(report.Code, element);
                            feedbackMessage = feedback.Message;
                            return feedback.Result == IgnoreElementResult.Success;
                        }, cancellationToken);

                    string message = !success
                        ? IgnoreElementFailedMessage + Environment.NewLine
                            + GetLocalizedString("details_message", feedbackMessage ?? string.Empty)
                        : IgnoreElementSucceededMessage;
                    _fixReportSender.Send(new FixReport(
                        report.Code, report.Document.Title, new(message, ("elementId", elementId))));

                    if (!success)
                    {
                        string resolvedMessage = message.Replace("{elementId}", elementId.ToString());
                        await _dialog.Show(new DialogRequest(resolvedMessage), cancellationToken);
                    }
                }
            };

            iconColor = System.Windows.Media.Color.FromRgb(0xFF, 0xB7, 0x4D);
            icon = new()
            {
                Kind = IconKind.Lightbulb,
                Foreground = new SolidColorBrush(iconColor)
            };
            async Task RunIgnoreFixAll(CancellationToken cancellationToken)
            {
                if (doc is null or { IsValidObject: false }) return;

                if (!element.IsValidObject) return;

                StringBuilder feedbackMessage = new();
                bool hasErrors = false;
                string transactionName = GetLocalizedString("ignoreElements_transaction");

                // The transaction is always committed (we return true) so that successfully ignored
                // elements are not rolled back just because some elements could not be ignored.
                await ExecuteTransaction(doc, transactionName, () => {
                        foreach (var reportItem in Collection)
                        {
                            if (reportItem.Code != report.Code) continue;
                            if (reportItem.Target is null) continue;

                            Element element = (Element)reportItem.Target;

                            if (!element.IsValidObject) continue;

                            try
                            {
                                var feedback = _ignoreElementProvider.Ignore(report.Code, element);
                                feedbackMessage.Append($"id {element.Id}: ").Append(feedback.Message);
                                feedbackMessage.Append(Environment.NewLine);
                                if (feedback.Result == IgnoreElementResult.Failed) hasErrors = true;
                            }
                            catch (Exception ex)
                            {
                                hasErrors = true;
                                feedbackMessage.Append($"id {element.Id}: {ex.Message}");
                                feedbackMessage.Append(Environment.NewLine);
                            }
                        }
                        return true;
                    }, cancellationToken);

                string message = GetLocalizedString(hasErrors
                    ? "ignoreElementsFailed_message"
                    : "ignoreElementsSucceeded_message");
                _fixReportSender.Send(new FixReport(
                    report.Code, report.Document.Title, new(message, ("elementId", elementId))));

                if (hasErrors)
                {
                    string dialogMessage = message + Environment.NewLine
                        + GetLocalizedString("details_message", feedbackMessage.ToString());

                    bool retry = await _confirmationDialog.Show(
                        new ConfirmationDialogRequest(dialogMessage, RetryTitle), cancellationToken);

                    // Already-ignored elements will disappear from the report (the diagnostic will no
                    // longer trigger for them), so a retry will only affect the remaining problem elements.
                    if (retry) await RunIgnoreFixAll(cancellationToken);
                }
            }

            FixViewModel ignoreFixAll = new()
            {
                Icon = icon,
                Title = GetLocalizedString("all_title", IgnoreFixTitle),
                FixDelegate = RunIgnoreFixAll
            };

            return GetCatalogSnapshot().ElementDiagnostics.SelectMany(registration => registration.Fixes)
                .Where(i => i.Identity.Code == report.Code)
                .SelectMany(i =>
                {
                    List<FixViewModel> fixes = [];

                    var iconColor = System.Windows.Media.Color.FromRgb(0xFF, 0xB7, 0x4D);
                    PathIcon icon = new()
                    {
                        Kind = IconKind.Lightbulb,
                        Foreground = new SolidColorBrush(iconColor)
                    };
                    FixViewModel fix = new() {
                        Icon = icon,
                        Title = i.Value,
                        FixDelegate = async (cancellationToken) => {
                            if (doc is null or { IsValidObject: false }) return;

                            if (!element.IsValidObject) return;

                            string transactionName = i.Value;
                            (bool success, string? error) = await ExecuteTransactionWithError(
                                doc, transactionName, () => ExecuteFix(
                                    i, CreateFixContext(report.Document, report.TargetDependencies, element), element),
                                cancellationToken);

                            string message = GetLocalizedString(success
                                ? "fixElementSucceeded_message"
                                : "fixElementFailed_message");
                            _fixReportSender.Send(new FixReport(
                                i.Identity.Code, report.Document.Title, new(message, ("elementId", elementId))));

                            if (!success)
                            {
                                string resolvedMessage = message.Replace("{elementId}", elementId.ToString());
                                string dialogMessage = string.IsNullOrWhiteSpace(error)
                                    ? resolvedMessage
                                    : resolvedMessage + Environment.NewLine
                                        + GetLocalizedString("details_message", error ?? string.Empty);
                                await _dialog.Show(new DialogRequest(dialogMessage), cancellationToken);
                            }
                        }
                    };
                    fixes.Add(fix);

                    iconColor = System.Windows.Media.Color.FromRgb(0xFF, 0xB7, 0x4D);
                    icon = new()
                    {
                        Kind = IconKind.Lightbulb,
                        Foreground = new SolidColorBrush(iconColor)
                    };
                    async Task RunFixAll(CancellationToken cancellationToken)
                    {
                        if (doc is null or { IsValidObject: false }) return;

                        StringBuilder errors = new();
                        bool hasErrors = false;
                        string transactionName = GetLocalizedString("all_title", i.Value);

                        // The transaction is always committed (we return true) so that successfully fixed
                        // elements are not rolled back just because some elements could not be fixed.
                        await ExecuteTransaction(doc, transactionName, () => {
                                foreach (var reportItem in Collection)
                                {
                                    if (reportItem.Code != report.Code) continue;
                                    if (reportItem.Target is null) continue;

                                    Element element = (Element)reportItem.Target;

                                    if (!element.IsValidObject) continue;

                                    try
                                    {
                                        bool result = ExecuteFix(
                                            i, CreateFixContext(doc, reportItem.TargetDependencies, element), element);
                                        if (!result)
                                        {
                                            hasErrors = true;
                                            errors.AppendLine(GetLocalizedString(
                                                "fixElementFailed_message") + $" (id: {element.Id})");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        hasErrors = true;
                                        errors.AppendLine($"id {element.Id}: {ex.Message}");
                                    }
                                }
                                return true;
                            }, cancellationToken);

                        string message = GetLocalizedString(hasErrors
                            ? "fixElementsFailed_message"
                            : "fixElementsSucceeded_message");
                        _fixReportSender.Send(new FixReport(i.Identity.Code, report.Document.Title, new(message)));

                        if (hasErrors)
                        {
                            string dialogMessage = message + Environment.NewLine
                                + GetLocalizedString("details_message", errors.ToString());

                            bool retry = await _confirmationDialog.Show(
                                new ConfirmationDialogRequest(dialogMessage, RetryTitle), cancellationToken);

                            // Already-fixed elements have become invalid (IsValidObject == false) and will
                            // be skipped on the next pass — a retry will only affect the remaining problem elements.
                            if (retry) await RunFixAll(cancellationToken);
                        }
                    }

                    FixViewModel fixAll = new() {
                        Icon = icon,
                        Title = GetLocalizedString("all_title", i.Value),
                        FixDelegate = RunFixAll
                    };
                    fixes.Add(fixAll);

                    return fixes;
                }).Append(ignoreFix).Append(ignoreFixAll).ToList();
        }
        else if (report.Target is Document document)
        {
            string documentTitle = document.Title;
            return GetCatalogSnapshot().DocumentDiagnostics.SelectMany(registration => registration.Fixes)
                .Where(i => i.Identity.Code == report.Code)
                .Select(i =>
                {
                    var iconColor = System.Windows.Media.Color.FromRgb(0xFF, 0xB7, 0x4D);
                    PathIcon icon = new()
                    {
                        Kind = IconKind.Lightbulb,
                        Foreground = new SolidColorBrush(iconColor)
                    };
                    FixViewModel fix = new() {
                        Icon = icon,
                        Title = i.Value,
                        FixDelegate = async (cancellationToken) => {
                            if (document is null or { IsValidObject: false }) return;

                            string transactionName = i.Value;
                            (bool success, string? error) = await ExecuteTransactionWithError(
                                document, transactionName, () => i.Execute(document), cancellationToken);

                            string message = GetLocalizedString(success
                                ? "fixDocumentSucceeded_message"
                                : "fixDocumentFailed_message");
                            _fixReportSender.Send(new FixReport(
                                i.Identity.Code, report.Document.Title,
                                new(message, ("documentTitle", documentTitle))));

                            if (!success)
                            {
                                string resolvedMessage = message.Replace("{documentTitle}", documentTitle);
                                string dialogMessage = string.IsNullOrWhiteSpace(error)
                                    ? resolvedMessage
                                    : resolvedMessage + Environment.NewLine
                                        + GetLocalizedString("details_message", error ?? string.Empty);
                                await _dialog.Show(new DialogRequest(dialogMessage), cancellationToken);
                            }
                        }
                    };
                    return fix;
                }).ToList();
        }
        return [];
    }

    private static ElementFixContext CreateFixContext(
        Document document,
        object[]? dependencies,
        Element target)
    {
        ElementId[] dependencyIds = dependencies?
            .OfType<Element>()
            .Where(dependency => dependency.IsValidObject && dependency.Document.Equals(document))
            .Select(dependency => dependency.Id)
            .ToArray() ?? [];
        var elementSets = new Dictionary<string, IReadOnlyCollection<ElementId>>
        {
            [ElementVisualizationSetKeys.Target] = [target.Id],
            [ElementVisualizationSetKeys.Dependencies] = dependencyIds
        };
        return new(document, elementSets);
    }

    private static bool ExecuteFix(IElementFix fix, ElementFixContext context, Element target) =>
        fix is IElementSetFix elementSetFix
            ? elementSetFix.Execute(context)
            : fix.Execute(target);

    private static async Task<bool> ExecuteTransaction(
        Document document, string transactionName, Func<bool> action, CancellationToken cancellationToken)
    {
        (bool success, _) = await ExecuteTransactionWithError(document, transactionName, action, cancellationToken);
        return success;
    }

    private static Task<(bool Success, string? Error)> ExecuteTransactionWithError(
        Document document, string transactionName, Func<bool> action, CancellationToken cancellationToken)
        => RevitTask.RunAsync(_ => {
            cancellationToken.ThrowIfCancellationRequested();

            using Transaction transaction = new(document, transactionName);
            try
            {
                if (transaction.Start() != TransactionStatus.Started)
                    return (false, (string?)GetLocalizedString("transactionStartFailed_message"));
                if (!action())
                    return (false, (string?)GetLocalizedString("operationFailed_message"));

                return (transaction.Commit() == TransactionStatus.Committed, (string?)null);
            }
            catch (Exception ex)
            {
                return (false, (string?)ex.Message);
            }
        });

    private DiagnosticCatalogSnapshot GetCatalogSnapshot() =>
        _catalogLease?.Snapshot ?? throw new InvalidOperationException("The diagnostic catalog is not initialized.");
}
