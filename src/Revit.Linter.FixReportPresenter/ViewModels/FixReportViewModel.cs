using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Revit.Context.Abstractions.Services;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.FixReportPresenter.ViewModels.Base;
using Revit.Linter.FixReportProvider.Abstractions.Models;
using Revit.Linter.FixReportProvider.Abstractions.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using TextRange = System.Windows.Documents.TextRange;

namespace Revit.Linter.FixReportPresenter.ViewModels;

[XamlConstructor]
internal sealed partial class FixReportViewModel : InitializableObservableObject
{
    private readonly IRevitContext _revitContext;
    private readonly IFixReportReceiver _fixReportReceiver;
    private readonly IEnumerable<IAccentElementsService> _accentElementsServices;

    public FixReportViewModel(
        IRevitContext revitContext, IFixReportReceiver fixReportReceiver, IEnumerable<IAccentElementsService> accentElementsServices)
    {
        _fixReportReceiver = fixReportReceiver;
        Collection = [];
        _revitContext = revitContext;
        _accentElementsServices = accentElementsServices;
    }

    [ObservableProperty]
    public partial ObservableCollection<FixReportItemViewModel> Collection { get; private set; }  = null!;
    partial void OnCollectionChanged(ObservableCollection<FixReportItemViewModel> value)
        => InitializeCollectionView();

    [ObservableProperty]
    public partial CollectionViewSource? CollectionViewSource { get; private set; }

    [ObservableProperty]
    public partial string SearchField { get; set; } = string.Empty;
    partial void OnSearchFieldChanged(string value) => RefreshCollectionView();

    [ObservableProperty]
    public partial IEnumerable<IFixListFilter> Filters { get; private set; } = [];
    partial void OnFiltersChanged(
        IEnumerable<IFixListFilter>? oldValue, IEnumerable<IFixListFilter> newValue)
    {
        if (oldValue != null)
            foreach (var filter in oldValue)
                filter.PropertyChanged -= Filter_PropertyChanged;
        if (newValue != null)
            foreach (var filter in newValue)
                filter.PropertyChanged += Filter_PropertyChanged;
        RefreshCollectionView();
    }
    private void Filter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        => RefreshCollectionView();

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

    private void InitializeCollectionView()
    {
        CollectionViewSource = new()
        {
            Source = Collection
        };

        CollectionViewSource.Filter += CollectionViewSource_Filter;

        CollectionViewSource.SortDescriptions.Clear();
        CollectionViewSource.SortDescriptions.Add(
            new SortDescription(nameof(FixReportItemViewModel.Code), ListSortDirection.Ascending));
    }
    private void RefreshCollectionView() => CollectionViewSource?.View.Refresh();

    private void CollectionViewSource_Filter(object sender, FilterEventArgs args)
       => args.Accepted = true
       && args.Item is FixReportItemViewModel viewModel
       //&& Filters.Where(i => i.IsActive).Any(filter => filter.IsValid(viewModel))
       && ((viewModel.Message.ToString() ?? string.Empty).Contains(SearchField, StringComparison.CurrentCultureIgnoreCase)
       || viewModel.Code.Contains(SearchField, StringComparison.CurrentCultureIgnoreCase))
        //todo viewModel.Message.ToString() возвращает не в том формате, что виден пользователю
        ;

    protected async override Task OnInitializing(CancellationToken cancellationToken = default)
    {
        await base.OnInitializing(cancellationToken);
        _fixReportReceiver.FixReportSent += FixReportReceiver_FixReportSent;
    }
    protected async override Task OnDeinitializing(CancellationToken cancellationToken = default)
    {
        await base.OnDeinitializing(cancellationToken);
        _fixReportReceiver.FixReportSent -= FixReportReceiver_FixReportSent;
    }

    private void FixReportReceiver_FixReportSent(object? sender, FixMessageSentEventArgs e)
    {
        var report = e.Report;
        FixReportItemViewModel item = new() {
            Created = report.Created,
            Code = report.Code, 
            Template = report.Message.Format,
            Args = report.Message.Args.ToDictionary(i => i.Item1, i => i.Item2),
            AccentElementDelegate = i => SelectElement(i),
        };
        Collection.Add(item);
    }
}
