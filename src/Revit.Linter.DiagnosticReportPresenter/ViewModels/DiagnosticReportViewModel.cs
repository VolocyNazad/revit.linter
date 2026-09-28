using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Revit.Async;
using Revit.Context.Abstractions.Services;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Diagnostic.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.DiagnosticReportPresenter.Interactions.Abstractions.Services;
using Revit.Linter.DiagnosticReportPresenter.ViewModels.Base;
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
using System.Net;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels;

internal sealed partial class DiagnosticReportViewModel : IDiagnosticReportPresenter
{
    public void Clear()
    {
        Collection.Clear();
        ClearFilters();
    }

    public void Clear(string documentTitle)
    {
        var toRemove = Collection.Where(i => i.DocumentTitle == documentTitle).ToList();

        foreach (var item in toRemove) Collection.Remove(item);

        ClearFilters();
    }


    public void Refresh()
    {
        RefreshFilters();
    }
}

[XamlConstructor]
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
    private readonly IConfirmationDialog _confirmationDialog;
    private IDiagnosticCatalogSnapshotLease? _catalogLease;
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
            IDialog dialog, IConfirmationDialog confirmationDialog) : base(idlingScheduler)
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
        _confirmationDialog = confirmationDialog;

        Collection = [];
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
    public partial IEnumerable<IDiagnosticReportFilter> Filters { get; set; } = [];
    partial void OnFiltersChanged(
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
        SaveFileDialog dialog = new()
        {
            AddExtension = true,
            DefaultExt = ".csv",
            FileName = CreateExportFileName(),
            Filter = "CSV (*.csv)|*.csv|JSON (*.json)|*.json|YAML (*.yaml)|*.yaml|HTML (*.html)|*.html",
            FilterIndex = 1,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true) return;

        List<DiagnosticReportExportItem> items = (CollectionViewSource?.View
            .Cast<object>()
            .OfType<DiagnosticReportItemViewModel>() ?? [])
            .Select(item => new DiagnosticReportExportItem(
                item.SeverityText,
                item.Code,
                item.MessageText,
                item.DocumentTitle,
                item.Created))
            .ToList();

        string extension = dialog.FilterIndex switch
        {
            2 => ".json",
            3 => ".yaml",
            4 => ".html",
            _ => ".csv"
        };
        string fileName = Path.ChangeExtension(dialog.FileName, extension);

        switch (dialog.FilterIndex)
        {
            case 2:
                ExportJson(fileName, items);
                break;
            case 3:
                ExportYaml(fileName, items);
                break;
            case 4:
                ExportHtml(fileName, items);
                break;
            default:
                ExportCsv(fileName, items);
                break;
        }
    }

    private void ExportCsv(string fileName, IEnumerable<DiagnosticReportExportItem> items)
    {
        string listSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        char delimiter = listSeparator.Length > 0 ? listSeparator[0] : ',';
        StringBuilder content = new();
        AppendCsvRow(content, delimiter,
            SeverityHeader, CodeHeader, MessageHeader, DocumentHeader, CreatedHeader);

        foreach (DiagnosticReportExportItem item in items)
            AppendCsvRow(content, delimiter,
                item.Severity,
                item.Code,
                item.Message,
                item.Document,
                item.Created.ToString("G", CultureInfo.CurrentCulture));

        File.WriteAllText(fileName, content.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static void ExportJson(string fileName, IEnumerable<DiagnosticReportExportItem> items)
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        File.WriteAllText(fileName, JsonSerializer.Serialize(items, options), new UTF8Encoding(false));
    }

    private static void ExportYaml(string fileName, IEnumerable<DiagnosticReportExportItem> items)
    {
        ISerializer serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        File.WriteAllText(fileName, serializer.Serialize(items), new UTF8Encoding(false));
    }

    private void ExportHtml(string fileName, IReadOnlyCollection<DiagnosticReportExportItem> items)
    {
        string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        string errorText = DiagnosticSeverityLocalizations.GetString(DiagnosticSeverity.Error.ToString());
        string warningText = DiagnosticSeverityLocalizations.GetString(DiagnosticSeverity.Warning.ToString());
        string messageText = DiagnosticSeverityLocalizations.GetString(DiagnosticSeverity.Message.ToString());
        int errorCount = items.Count(item => item.Severity == errorText);
        int warningCount = items.Count(item => item.Severity == warningText);
        int messageCount = items.Count(item => item.Severity == messageText);
        string targetDocumentTitle = TargetDocumentTitle ?? string.Empty;
        string document = string.IsNullOrWhiteSpace(targetDocumentTitle)
            ? HtmlAllDocumentsText
            : targetDocumentTitle;

        StringBuilder content = new();
        content.AppendLine("<!DOCTYPE html>")
            .Append("<html lang=\"").Append(Encode(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)).AppendLine("\">")
            .AppendLine("<head>")
            .AppendLine("<meta charset=\"utf-8\">")
            .AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">")
            .Append("<title>").Append(Encode(HtmlReportTitle)).AppendLine("</title>")
            .AppendLine("<style>")
            .AppendLine("body{margin:0;background:#f5f7fa;color:#172033;font-family:Segoe UI,Arial,sans-serif;font-size:14px}")
            .AppendLine("main{max-width:1200px;margin:0 auto;padding:32px 24px 48px}")
            .AppendLine("h1{margin:0 0 8px;font-size:28px}h2{margin:32px 0 12px;font-size:18px}")
            .AppendLine(".meta{color:#5d6678;margin-bottom:24px}.meta span+span:before{content:' · ';padding:0 6px}")
            .AppendLine(".cards{display:grid;grid-template-columns:repeat(4,minmax(130px,1fr));gap:12px}")
            .AppendLine(".card{background:#fff;border:1px solid #dfe3eb;border-radius:8px;padding:16px}.card strong{display:block;font-size:26px;margin-bottom:4px}")
            .AppendLine(".error{border-top:4px solid #c62828}.warning{border-top:4px solid #ef8c00}.message{border-top:4px solid #1976d2}.total{border-top:4px solid #48566a}")
            .AppendLine(".table-wrap{overflow-x:auto;background:#fff;border:1px solid #dfe3eb;border-radius:8px}")
            .AppendLine("table{width:100%;border-collapse:collapse}th,td{padding:10px 12px;text-align:left;vertical-align:top;border-bottom:1px solid #e6e9ef}th{background:#eef1f6;white-space:nowrap}tr:last-child td{border-bottom:0}.count{width:1%;text-align:right}.message-cell{white-space:pre-wrap;min-width:320px}")
            .AppendLine(".empty{padding:24px;text-align:center;color:#5d6678}")
            .AppendLine("@media(max-width:700px){main{padding:20px 12px}.cards{grid-template-columns:repeat(2,1fr)}}")
            .AppendLine("@media print{body{background:#fff}main{max-width:none;padding:0}.card,.table-wrap{break-inside:avoid}.table-wrap{overflow:visible}}")
            .AppendLine("</style>")
            .AppendLine("</head>")
            .AppendLine("<body><main>")
            .Append("<h1>").Append(Encode(HtmlReportTitle)).AppendLine("</h1>")
            .Append("<div class=\"meta\"><span>").Append(Encode(DocumentHeader)).Append(": ")
            .Append(Encode(document)).Append("</span><span>").Append(Encode(HtmlGeneratedLabel)).Append(": ")
            .Append(Encode(DateTime.Now.ToString("G", CultureInfo.CurrentCulture))).AppendLine("</span></div>")
            .AppendLine("<section class=\"cards\">");

        AppendHtmlSummaryCard(content, "total", HtmlTotalLabel, items.Count);
        AppendHtmlSummaryCard(content, "error", errorText, errorCount);
        AppendHtmlSummaryCard(content, "warning", warningText, warningCount);
        AppendHtmlSummaryCard(content, "message", messageText, messageCount);

        content.AppendLine("</section>")
            .Append("<h2>").Append(Encode(HtmlSummaryByCodeTitle)).AppendLine("</h2>")
            .AppendLine("<div class=\"table-wrap\"><table><thead><tr>")
            .Append("<th>").Append(Encode(CodeHeader)).Append("</th><th class=\"count\">")
            .Append(Encode(HtmlCountHeader)).AppendLine("</th></tr></thead><tbody>");

        foreach (IGrouping<string, DiagnosticReportExportItem> group in items
                     .GroupBy(item => item.Code)
                     .OrderByDescending(group => group.Count())
                     .ThenBy(group => group.Key, StringComparer.CurrentCulture))
        {
            content.Append("<tr><td>").Append(Encode(group.Key)).Append("</td><td class=\"count\">")
                .Append(group.Count().ToString(CultureInfo.CurrentCulture)).AppendLine("</td></tr>");
        }

        content.AppendLine("</tbody></table></div>")
            .Append("<h2>").Append(Encode(HtmlDetailsTitle)).AppendLine("</h2>");

        if (items.Count == 0)
        {
            content.Append("<div class=\"table-wrap empty\">").Append(Encode(HtmlNoResultsText)).AppendLine("</div>");
        }
        else
        {
            content.AppendLine("<div class=\"table-wrap\"><table><thead><tr>")
                .Append("<th>").Append(Encode(SeverityHeader)).Append("</th><th>").Append(Encode(CodeHeader))
                .Append("</th><th>").Append(Encode(MessageHeader)).Append("</th><th>").Append(Encode(DocumentHeader))
                .Append("</th><th>").Append(Encode(CreatedHeader)).AppendLine("</th></tr></thead><tbody>");

            foreach (DiagnosticReportExportItem item in items)
            {
                content.Append("<tr><td>").Append(Encode(item.Severity)).Append("</td><td>")
                    .Append(Encode(item.Code)).Append("</td><td class=\"message-cell\">")
                    .Append(Encode(item.Message)).Append("</td><td>").Append(Encode(item.Document))
                    .Append("</td><td>").Append(Encode(item.Created.ToString("G", CultureInfo.CurrentCulture)))
                    .AppendLine("</td></tr>");
            }

            content.AppendLine("</tbody></table></div>");
        }

        content.AppendLine("</main></body></html>");
        File.WriteAllText(fileName, content.ToString(), new UTF8Encoding(false));
    }

    private static void AppendHtmlSummaryCard(StringBuilder content, string style, string title, int count)
    {
        content.Append("<div class=\"card ").Append(style).Append("\"><strong>")
            .Append(count.ToString(CultureInfo.CurrentCulture)).Append("</strong><span>")
            .Append(WebUtility.HtmlEncode(title)).AppendLine("</span></div>");
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

    private static void AppendCsvRow(StringBuilder builder, char delimiter, params string?[] values)
    {
        for (int index = 0; index < values.Length; index++)
        {
            if (index > 0) builder.Append(delimiter);

            string value = values[index] ?? string.Empty;
            bool requiresEscaping = value.Contains(delimiter)
                || value.Contains('"')
                || value.Contains('\r')
                || value.Contains('\n');
            if (!requiresEscaping)
            {
                builder.Append(value);
                continue;
            }

            builder.Append('"');
            builder.Append(value.Replace("\"", "\"\""));
            builder.Append('"');
        }

        builder.AppendLine();
    }

    private sealed record DiagnosticReportExportItem(
        string Severity,
        string Code,
        string Message,
        string Document,
        DateTime Created);

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
        _catalogChangesEnabled = false;
        _diagnosticCatalog.Changed -= DiagnosticCatalog_Changed;
        _diagnosticReportReceiver.ReportSent -= DiagnosticReportReceiver_DiagnosticReportSent;
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


        // todo Need to account for dependent elements when processing changes. For collisions, for example. Add another field
        // todo simplify
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
    {
        DiagnosticReport report = e.Report;

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
            Fixes = CreateFixes(report),
            Args = report.Message.Args.ToDictionary(i => i.Item1, i => i.Item2),
            Severity = report.Severity,
            DocumentTitle = report.Document.Title,
            AccentElementDelegate = i => SelectElement(i),
            IsObsolete = report.IsObsolete,
            ObsoleteDescription = report.ObsoleteDescription,
        };
        Collection.Add(item);
    }

    private List<FixViewModel> CreateFixes(DiagnosticReport report)
    {
        if (report.Target is Element element)
        {
            var doc = report.Document;

            var elementId = element.Id;

            var iconColor = System.Windows.Media.Color.FromRgb(0xFF, 0xB7, 0x4D);
            PackIcon icon = new()
            {
                Kind = PackIconKind.Idea,
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
                Kind = PackIconKind.Idea,
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
                    PackIcon icon = new()
                    {
                        Kind = PackIconKind.Idea,
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
                                doc, transactionName, () => i.Execute(element), cancellationToken);

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
                                    : resolvedMessage + Environment.NewLine + GetLocalizedString("details_message", error!);
                                await _dialog.Show(new DialogRequest(dialogMessage), cancellationToken);
                            }
                        }
                    };
                    fixes.Add(fix);

                    iconColor = System.Windows.Media.Color.FromRgb(0xFF, 0xB7, 0x4D);
                    icon = new()
                    {
                        Kind = PackIconKind.Idea,
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
                                        bool result = i.Execute(element);
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
                    PackIcon icon = new()
                    {
                        Kind = PackIconKind.Idea,
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
                                    : resolvedMessage + Environment.NewLine + GetLocalizedString("details_message", error!);
                                await _dialog.Show(new DialogRequest(dialogMessage), cancellationToken);
                            }
                        }
                    };
                    return fix;
                }).ToList();
        }
        return [];
    }

    private async Task<bool> ExecuteTransaction(
        Document document, string transactionName, Func<bool> action, CancellationToken cancellationToken)
    {
        (bool success, _) = await ExecuteTransactionWithError(document, transactionName, action, cancellationToken);
        return success;
    }

    private Task<(bool Success, string? Error)> ExecuteTransactionWithError(
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
