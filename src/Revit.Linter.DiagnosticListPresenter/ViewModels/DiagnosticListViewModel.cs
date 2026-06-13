using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.DiagnosticListPresenter.ViewModels.Base;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace Revit.Linter.DiagnosticListPresenter.ViewModels;

[XamlConstructor]
[AutoConstructor]
internal sealed partial class DiagnosticListViewModel : InitializableObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<ElementDiagnosticIdOverrides> _elementDiagnosticIdOverrides;
    private readonly IEnumerable<DocumentDiagnosticIdOverrides> _documentDiagnosticIdOverrides;

    [ObservableProperty]
    private ObservableCollection<DiagnosticItemViewModel> _collection = null!;
    partial void OnCollectionChanged(ObservableCollection<DiagnosticItemViewModel> value)
        => InitializeCollectionView();

    [ObservableProperty]
    private CollectionViewSource? _collectionViewSource;

    [ObservableProperty]
    private string _searchField = string.Empty;
    partial void OnSearchFieldChanged(string value) => RefreshCollectionView();

    [ObservableProperty]
    private IEnumerable<IDiagnosticListFilter> _filters = [];
    partial void OnFiltersChanged(
        IEnumerable<IDiagnosticListFilter>? oldValue, IEnumerable<IDiagnosticListFilter> newValue)
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

    #region [CheckAll] Command - Выделить все

    /// <summary> Выделить все </summary>
    [RelayCommand]
    private void CheckAll()
    {
        var collection = Collection;
        foreach (var item in collection)
            item.IsActive = true;
    }

    #endregion

    #region [UncheckAll] Command - Снять все

    /// <summary> Снять все </summary>
    [RelayCommand]
    private void UncheckAll()
    {
        var collection = Collection;
        foreach (var item in collection)
            item.IsActive = false;
    }

    #endregion

    #region [InvertAll] Command - Инвертировать все

    /// <summary> Инвертировать все </summary>
    [RelayCommand]
    private void InvertAll()
    {
        var collection = Collection;
        foreach (var item in collection)
            item.IsActive = !item.IsActive;
    }

    #endregion

    #region [CheckVisible] Command - Выделить видимое

    /// <summary> Выделить видимое </summary>
    [RelayCommand]
    private void CheckVisible()
    {
        var collection = CollectionViewSource!.View;
        foreach (DiagnosticItemViewModel item in collection)
            item.IsActive = true;
    }

    #endregion

    #region [UncheckVisible] Command - Снять видимое

    /// <summary> Снять видимое </summary>
    [RelayCommand]
    private void UncheckVisible()
    {
        var collection = CollectionViewSource!.View;
        foreach (DiagnosticItemViewModel item in collection)
            item.IsActive = false;
    }

    #endregion

    #region [InvertVisible] Command - Инвертировать видимое

    /// <summary> Инвертировать видимое </summary>
    [RelayCommand]
    private void InvertVisible()
    {
        var collection = CollectionViewSource!.View;
        foreach (DiagnosticItemViewModel item in collection)
            item.IsActive = !item.IsActive;
    }

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
            new SortDescription(nameof(DiagnosticItemViewModel.Code), ListSortDirection.Ascending));

        CollectionViewSource.GroupDescriptions.Clear();
        CollectionViewSource.GroupDescriptions.Add(
            new PropertyGroupDescription(nameof(DiagnosticItemViewModel.TargetType)));
    }
    private void RefreshCollectionView() => CollectionViewSource?.View.Refresh();

    private void CollectionViewSource_Filter(object sender, FilterEventArgs args)
        => args.Accepted = true
        && args.Item is DiagnosticItemViewModel viewModel
        //&& Filters.Where(i => i.IsActive).Any(filter => filter.IsValid(viewModel))
        && (viewModel.Description.ToString().Contains(SearchField, StringComparison.CurrentCultureIgnoreCase)
        || viewModel.Code.Contains(SearchField, StringComparison.CurrentCultureIgnoreCase));

    protected override async Task OnInitializing(CancellationToken cancellationToken)
    {
        await base.OnInitializing(cancellationToken);
        List<DiagnosticItemViewModel> items = [];
        foreach (var item in _elementDiagnosticIdOverrides)
        {
            var viewModel = _serviceProvider.GetRequiredService<DiagnosticItemViewModel>();
            var diagnosticId  = item.Identity;
            viewModel.TargetType = TargetType.Element;
            viewModel.Code = diagnosticId.Code;
            viewModel.Description = diagnosticId.Description;
            viewModel.IsActive = item.IsActive;
            viewModel.Severity = item.Severity;
            viewModel.IsObsolete = diagnosticId.IsObsolete;
            viewModel.ObsoleteDescription = diagnosticId.ObsoleteDescription;
            items.Add(viewModel);
        }
        foreach (var item in _documentDiagnosticIdOverrides)
        {
            var viewModel = _serviceProvider.GetRequiredService<DiagnosticItemViewModel>();
            var diagnosticId = item.Identity;
            viewModel.TargetType = TargetType.Document;
            viewModel.Code = diagnosticId.Code;
            viewModel.Description = diagnosticId.Description;
            viewModel.IsActive = item.IsActive;
            viewModel.Severity = item.Severity;
            viewModel.IsObsolete = diagnosticId.IsObsolete;
            viewModel.ObsoleteDescription = diagnosticId.ObsoleteDescription;
            items.Add(viewModel);
        }

        Collection = new(items);

    }
}
