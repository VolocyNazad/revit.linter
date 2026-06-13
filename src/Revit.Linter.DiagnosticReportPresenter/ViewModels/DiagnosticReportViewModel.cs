using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Revit.Context.Abstractions.Services;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DiagnosticReportPresenter.ViewModels.Base;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Services;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using TextRange = System.Windows.Documents.TextRange;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels;

[XamlConstructor]
internal sealed partial class DiagnosticReportViewModel : RevitInteractionViewModel
{
    private readonly IEnumerable<IAccentElementsService> _accentElementsServices;
    private readonly IDiagnosticReportReceiver _diagnosticReportReceiver;
    private readonly IDiagnosticService _diagnosticService;

    public DiagnosticReportViewModel(
            IRevitContext revitContext, IEnumerable<IAccentElementsService> accentElementsServices,
            IDiagnosticService diagnosticService,
            IDiagnosticReportReceiver diagnosticReportReceiver) : base(revitContext)
    {
        _accentElementsServices = accentElementsServices;
        _diagnosticReportReceiver = diagnosticReportReceiver;
        _diagnosticService = diagnosticService;

        Collection = [];
    }

    [ObservableProperty]
    private ObservableCollection<DiagnosticReportItemViewModel> _collection = null!;
    partial void OnCollectionChanged(ObservableCollection<DiagnosticReportItemViewModel> value)
        => InitializeCollectionView();

    [ObservableProperty]
    private CollectionViewSource? _collectionViewSource;

    [ObservableProperty]
    private string _searchField = string.Empty;
    partial void OnSearchFieldChanged(string value) => RefreshCollectionView();

    [ObservableProperty]
    private string _diagnosticTime = string.Empty;

    [ObservableProperty]
    public partial IEnumerable<IDiagnosticReportFilter> SeverityFilters { get; set; } = [];
    partial void OnSeverityFiltersChanged(
        IEnumerable<IDiagnosticReportFilter>? oldValue, IEnumerable<IDiagnosticReportFilter> newValue)
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
        IEnumerable<IDiagnosticReportFilter>? oldValue, IEnumerable<IDiagnosticReportFilter> newValue)
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
    private bool _onActiveViewMode = false;

    #region [RunDiagnostic] Command - Запустить диагностику  

    /// <summary> Запустить диагностику </summary>
    [RelayCommand(CanExecute = nameof(CanRunDiagnostic))]
    private async Task RunDiagnostic(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        RunDiagnostic();
        DiagnosticTime = $"{stopwatch.Elapsed.Seconds} sec.";
        stopwatch.Stop();
    }

    private void RunDiagnostic()
    {
        ClearReport();

        Document? targetDocument = _revitContext.ActiveDocument;
        if (targetDocument is null) return;

        View? targetView = OnActiveViewMode ? targetDocument.ActiveView : null;

        _diagnosticService.Excecute(targetDocument, targetView);

        RefreshFilters();
    }

    private bool CanRunDiagnostic() => _revitContext.ActiveDocument is { IsFamilyDocument: false };

    #endregion

    #region [ShowElement] Command - Показать элемент  

    /// <summary> Показать элемент </summary>
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
        => elementId is int
        && _revitContext.ActiveDocument is { IsFamilyDocument: false };

    #endregion

    #region [SelectElement] Command - Выбрать элемент  

    /// <summary> Выбрать элемент </summary>
    [RelayCommand(CanExecute = nameof(CanSelectElement))]
    private void SelectElement(object? parameter)
    {
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
        => elementId is int
        && _revitContext.ActiveDocument is { IsFamilyDocument: false };

    #endregion

    #region [IsolateElementsOnView] Command - Изолировать элемент на активном виде 

    /// <summary> Изолировать элемент на активном виде </summary>
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
        => elementId is int
        && _revitContext.ActiveDocument is { IsFamilyDocument: false }
        && _revitContext.ActiveDocument.ActiveView is not null;

    #endregion

    #region [CutViewByElement] Command - Обрезать активный 3D-вид по элементу 

    /// <summary> Обрезать активный 3D-вид по элементу </summary>
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
        => elementId is int
        && _revitContext.ActiveDocument is { IsFamilyDocument: false }
        && _revitContext.ActiveDocument.ActiveView is View3D;

    #endregion

    #region [CopyToClipboard] Command - Копировать текст в буфер обмена

    /// <summary> Копировать текст в буфер обмена </summary>
    [RelayCommand]
    private void CopyToClipboard(object item)
    {
        string text = item is FlowDocument flowDocument
            ? new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd).Text
            : item.ToString();
        Clipboard.SetText(text.ToString());
    }

    #endregion

    private void Filter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        => RefreshCollectionView();

    private void InitializeCollectionView()
    {
        CollectionViewSource = new() {
            Source = Collection
        };

        CollectionViewSource.Filter += CollectionViewSource_Filter;

        CollectionViewSource.SortDescriptions.Clear();
        CollectionViewSource.SortDescriptions.Add(
            new SortDescription(nameof(DiagnosticReportItemViewModel.Code), ListSortDirection.Ascending));
    }
    private void RefreshCollectionView() => CollectionViewSource?.View.Refresh();
    private void CollectionViewSource_Filter(object sender, FilterEventArgs args)
        => args.Accepted = true
        && args.Item is DiagnosticReportItemViewModel viewModel
        && SeverityFilters.Where(i => i.IsActive).Any(filter => filter.IsValid(viewModel))
        && Filters.Where(i => i.IsActive).Any(filter => filter.IsValid(viewModel))
        && ((viewModel.Message.ToString() ?? string.Empty).Contains(SearchField, StringComparison.CurrentCultureIgnoreCase)
        || viewModel.Code.Contains(SearchField, StringComparison.CurrentCultureIgnoreCase))
        //todo viewModel.Message.ToString() возвращает не в том формате, что виден пользователю
        ;

    private void Remove(DiagnosticReportItemViewModel item)
    {
        Collection.Remove(item);
        RefreshFilters();
    }
    private void ClearReport()
    {
        Collection.Clear();
        ClearFilters();
    }
    private void ClearFilters()
    {
        SeverityFilters = [];
        Filters = [];
    }

    private void RefreshFilters()
    {
        List<IDiagnosticReportFilter> severityFilters = [];

        severityFilters.Add(new DiagnosticReportSeverityFilterViewModel(true, DiagnosticSeverity.Message, Collection.Count(i => i.Severity == DiagnosticSeverity.Message)));
        severityFilters.Add(new DiagnosticReportSeverityFilterViewModel(true, DiagnosticSeverity.Warning, Collection.Count(i => i.Severity == DiagnosticSeverity.Warning)));
        severityFilters.Add(new DiagnosticReportSeverityFilterViewModel(true, DiagnosticSeverity.Error, Collection.Count(i => i.Severity == DiagnosticSeverity.Error)));

        SeverityFilters = severityFilters;

        List<IDiagnosticReportFilter> filters = [];

        filters.Add(new DiagnosticReportObsoleteFilterViewModel(true, Collection.Count(i => i.IsObsolete)));
        filters.Add(new DiagnosticReportActualFilterViewModel(true, Collection.Count(i => !i.IsObsolete)));

        Filters = filters;
    }

    protected async override Task OnInitializing(CancellationToken cancellationToken = default)
    {
        await base.OnInitializing(cancellationToken);
        _diagnosticReportReceiver.DiagnosticReportSent += DiagnosticReportReceiver_DiagnosticReportSent;
    }
    protected async override Task OnDeinitializing(CancellationToken cancellationToken = default)
    {
        await base.OnDeinitializing(cancellationToken);
        _diagnosticReportReceiver.DiagnosticReportSent -= DiagnosticReportReceiver_DiagnosticReportSent;
    }
    private void DiagnosticReportReceiver_DiagnosticReportSent(object? sender, MessageSentEventArgs e)
    {
        DiagnosticReport report = e.Report;
        DiagnosticReportItemViewModel item = new() {
            Code = report.Code,
            Template = report.Message.Format,
            Args = report.Message.Args.ToDictionary(i => i.Item1, i => i.Item2),
            Severity = report.Severity,
            DocumentTitle = report.DocumentTitle,
            AccentElementDelegate = (i) => SelectElement(i),
            IsObsolete = report.IsObsolete,
            ObsoleteDescription = report.ObsoleteDescription,
        };
        Collection.Add(item);
    }

    protected override void OnRevitChanged() {
        RunDiagnosticCommand.NotifyCanExecuteChanged();
        ShowElementCommand.NotifyCanExecuteChanged();
        SelectElementCommand.NotifyCanExecuteChanged();
        IsolateElementsOnViewCommand.NotifyCanExecuteChanged();
        CutViewByElementCommand.NotifyCanExecuteChanged();
        //ClearReport();
    }
}