using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DiagnosticListPresenter.ViewModels.Base;
using Revit.Linter.ValueStore.Abstractions.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace Revit.Linter.DiagnosticListPresenter.ViewModels;

[XamlConstructor]
[AutoConstructor]
internal sealed partial class DiagnosticListViewModel : InitializableObservableObject // todo Сворачиваемость групп
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDiagnosticCatalog _diagnosticCatalog;
    private readonly IValueStore<ElementDiagnosticOverridesSettings> _elementOverrideStore;
    private readonly IValueStore<DocumentDiagnosticOverridesSettings> _documentOverrideStore;

    [ObservableProperty]
    public partial ObservableCollection<DiagnosticItemViewModel> Collection { get; private set; } = null!;
    partial void OnCollectionChanged(ObservableCollection<DiagnosticItemViewModel> value)
        => InitializeCollectionView();

    [ObservableProperty]
    public partial CollectionViewSource? CollectionViewSource { get; private set; }

    [ObservableProperty]
    public partial string SearchField { get; set; } = string.Empty;
    partial void OnSearchFieldChanged(string value) => RefreshCollectionView();

    [ObservableProperty]
    public partial IEnumerable<IDiagnosticListFilter> Filters { get; private set; } = [];
    partial void OnFiltersChanged(
        IEnumerable<IDiagnosticListFilter> oldValue, IEnumerable<IDiagnosticListFilter> newValue)
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
        => UpdateIsActive(Collection, _ => true);

    #endregion

    #region [UncheckAll] Command - Снять все

    /// <summary> Снять все </summary>
    [RelayCommand]
    private void UncheckAll()
        => UpdateIsActive(Collection, _ => false);

    #endregion

    #region [InvertAll] Command - Инвертировать все

    /// <summary> Инвертировать все </summary>
    [RelayCommand]
    private void InvertAll()
        => UpdateIsActive(Collection, value => !value);

    #endregion

    #region [CheckVisible] Command - Выделить видимое

    /// <summary> Выделить видимое </summary>
    [RelayCommand]
    private void CheckVisible()
        => UpdateIsActive(CollectionViewSource!.View.Cast<DiagnosticItemViewModel>(), _ => true);

    #endregion

    #region [UncheckVisible] Command - Снять видимое

    /// <summary> Снять видимое </summary>
    [RelayCommand]
    private void UncheckVisible()
        => UpdateIsActive(CollectionViewSource!.View.Cast<DiagnosticItemViewModel>(), _ => false);

    #endregion

    #region [InvertVisible] Command - Инвертировать видимое

    /// <summary> Инвертировать видимое </summary>
    [RelayCommand]
    private void InvertVisible()
        => UpdateIsActive(CollectionViewSource!.View.Cast<DiagnosticItemViewModel>(), value => !value);

    #endregion

    private void UpdateIsActive(
        IEnumerable<DiagnosticItemViewModel> source,
        Func<bool, bool> transform)
    {
        var items = source.ToArray();
        var elements = items.Where(item => item.TargetType == TargetType.Element).ToArray();
        var documents = items.Where(item => item.TargetType == TargetType.Document).ToArray();

        if (elements.Length > 0)
            _elementOverrideStore.Update(settings => UpdateSettings(settings.Overrides, elements, transform));
        if (documents.Length > 0)
            _documentOverrideStore.Update(settings => UpdateSettings(settings.Overrides, documents, transform));
    }

    private static void UpdateSettings(
        IDictionary<string, DiagnosticOverrideSettings> settings,
        IEnumerable<DiagnosticItemViewModel> items,
        Func<bool, bool> transform)
    {
        foreach (var item in items)
            settings[item.Code] = new DiagnosticOverrideSettings
            {
                Severity = item.Severity,
                IsActive = transform(item.IsActive),
            };
    }

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
        => args.Accepted = args.Item is DiagnosticItemViewModel viewModel
        //&& Filters.Where(i => i.IsActive).Any(filter => filter.IsValid(viewModel))
        && (viewModel.Description.ToString().Contains(SearchField, StringComparison.CurrentCultureIgnoreCase)
        || viewModel.Code.Contains(SearchField, StringComparison.CurrentCultureIgnoreCase));

    protected override async Task OnInitializing(CancellationToken cancellationToken = default)
    {
        await base.OnInitializing(cancellationToken);
        List<DiagnosticItemViewModel> items = [];
        foreach (ElementDiagnosticRegistration registration in _diagnosticCatalog.ElementDiagnostics)
        {
            var viewModel = _serviceProvider.GetRequiredService<DiagnosticItemViewModel>();
            viewModel.Initialize(registration.Override);
            items.Add(viewModel);
        }
        foreach (DocumentDiagnosticRegistration registration in _diagnosticCatalog.DocumentDiagnostics)
        {
            var viewModel = _serviceProvider.GetRequiredService<DiagnosticItemViewModel>();
            viewModel.Initialize(registration.Override);
            items.Add(viewModel);
        }

        Collection = new(items);

    }
}
