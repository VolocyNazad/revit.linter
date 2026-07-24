using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using MediatR;
using Revit.Context.Abstractions.Services;
using Revit.Events.Abstractions.Services;
using Revit.Linter.Diagnostic.Abstractions.Services;
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
using Revit.MediatR.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

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
internal sealed partial class DiagnosticReportViewModel : RevitInteractionViewModel
{
    private readonly IMediator _mediator;
    private readonly IRevitContext _revitContext;
    private readonly IFixReportSender _fixReportSender;
    private readonly IEnumerable<IAccentElementsService> _accentElementsServices;
    private readonly IDiagnosticReportReceiver _diagnosticReportReceiver;
    private readonly IElementChangesReceiver _elementChangesReceiver;
    private readonly IEnumerable<IElementFix> _elementFixes;
    private readonly IEnumerable<IDocumentFix> _documentFixes;
    private readonly IDiagnosticService _diagnosticService;
    private readonly IIgnoreElementProvider _ignoreElementProvider;

    public DiagnosticReportViewModel(
            IRevitContext revitContext, IAsyncExternalEvent externalEvent, IMediator mediator,
            IEnumerable<IAccentElementsService> accentElementsServices,
            IFixReportSender fixReportSender,
            IDiagnosticReportReceiver diagnosticReportReceiver, IElementChangesReceiver elementChangesReceiver,
            IEnumerable<IElementFix> elementFixes, IEnumerable<IDocumentFix> documentFixes,
            IDiagnosticService diagnosticService, IIgnoreElementProvider ignoreElementProvider) : base(externalEvent)
    {
        _accentElementsServices = accentElementsServices;
        _diagnosticReportReceiver = diagnosticReportReceiver;
        _elementChangesReceiver = elementChangesReceiver;
        _revitContext = revitContext;
        _fixReportSender = fixReportSender;
        _mediator = mediator;
        _elementFixes = elementFixes;
        _documentFixes = documentFixes;
        _diagnosticService = diagnosticService;
        _ignoreElementProvider = ignoreElementProvider;

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
    private void CopyToClipboard(object item) => Clipboard.SetText(item.ToString() ?? string.Empty);

    #endregion

    #region [Export] Command - Экспортировать

    /// <summary> Экспортировать </summary>
    [RelayCommand]
    private void Export()
    {
        // todo Реализовать
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
            && (string.IsNullOrEmpty(TargetDocumentTitle) || TargetDocumentTitle!.Equals(viewModel.DocumentTitle))
            && SeverityFilters.Where(i => i.IsActive).Any(filter => filter.IsValid(viewModel))
            && Filters.Where(i => i.IsActive).Any(filter => filter.IsValid(viewModel))
            && ((viewModel.Message.ToString() ?? string.Empty).Contains(SearchField, StringComparison.CurrentCultureIgnoreCase)
            || viewModel.Code.Contains(SearchField, StringComparison.CurrentCultureIgnoreCase))
            //todo viewModel.Message.ToString() возвращает не в том формате, что виден пользователю
            ;
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
        await base.OnInitializing(cancellationToken);

        _diagnosticReportReceiver.ReportSent += DiagnosticReportReceiver_DiagnosticReportSent;
        _elementChangesReceiver.Sent += ElementChangesReceiver_ElementChangesSent;

        TargetDocumentTitle = _revitContext.ActiveDocument?.Title;

        ShowElementCommand.NotifyCanExecuteChanged();
        SelectElementCommand.NotifyCanExecuteChanged();
        IsolateElementsOnViewCommand.NotifyCanExecuteChanged();
        CutViewByElementCommand.NotifyCanExecuteChanged();
    }

    protected async override Task OnDeinitializing(CancellationToken cancellationToken = default)
    {
        await base.OnDeinitializing(cancellationToken);

        _diagnosticReportReceiver.ReportSent -= DiagnosticReportReceiver_DiagnosticReportSent;
        _elementChangesReceiver.Sent -= ElementChangesReceiver_ElementChangesSent;
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
        IList<ElementId> toRefresh = [];

        var changes = e.Changes;

        Document? targetDocument = changes.Document;
        if (targetDocument is not { IsValidObject: true }) return;

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

        _diagnosticService.Execute(targetDocument, toRefresh);

        List<DiagnosticReportItemViewModel> GetTargetItemsBy(ElementId id) {
            return Collection
                .Where(i => i.DocumentTitle == targetDocument.Title
                && (IsTarget(i.Target, id) || (i.TargetDependencies != null && i.TargetDependencies.Any(dependency => IsTarget(dependency, id)))))
                .ToList();
        }

        static bool IsTarget(object? target, ElementId id)
            => target is Element { IsValidObject: true } element && element.Id.Equals(id);


        // todo Нужно учитывать зависимые элеметы при обработке изменений. При коллизиях например. Создать еще поле
        // todo упростить
    }

    private void DiagnosticReportReceiver_DiagnosticReportSent(object? sender, DiagnosticMessageSentEventArgs e)
    {
        DiagnosticReport report = e.Report;

        DiagnosticReportItemViewModel item = new() {
            Created = report.Created,
            Code = report.Code,
            Template = report.Message.Format,
            Target = report.Target,
            TargetDependencies = report.TargetDependencies,
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
                Title = "Игнорировать",
                FixDelegate = async (cancellationToken) => 
                {
                    if (doc is null or { IsValidObject: false }) return;

                    if (!element.IsValidObject) return;

                    string? feedbackMessage = null;
                    string transactionName = "Игнорирование проверки для элемента";
                    LambdaExternalEventTransactionCommand command = new(
                        doc, transactionName, async (_, _) => {
                            var feedback = _ignoreElementProvider.Ignore(report.Code, element);
                            feedbackMessage = feedback.Message;
                            return feedback.Result == IgnoreElementResult.Success;
                        });
                    var response = await _mediator.Send(command);

                    string message = response is { Result: false } or { HasError: true }
                        ? "Something went wrong while attempting to ignore the element with id: '{elementId}'. " + $"Details: {feedbackMessage}"
                        : "The element with id: '{elementId}' has been successfully ignored.";
                    _fixReportSender.Send(new FixReport(
                        report.Code, report.Document.Title, new(message, ("elementId", elementId))));
                }
            };

            iconColor = System.Windows.Media.Color.FromRgb(0xFF, 0xB7, 0x4D);
            icon = new()
            {
                Kind = PackIconKind.Idea,
                Foreground = new SolidColorBrush(iconColor)
            };
            FixViewModel ignoreFixAll = new()
            {
                Icon = icon,
                Title = "Игнорировать (all)",
                FixDelegate = async (cancellationToken) =>
                {
                    if (doc is null or { IsValidObject: false }) return;

                    if (!element.IsValidObject) return;

                    StringBuilder? feedbackMessage = new();
                    string transactionName = "Игнорирование провери для элементов";
                    LambdaExternalEventTransactionCommand command = new(
                        doc, transactionName, async (_, _) => {
                            bool hasErrors = false;
                            foreach (var reportItem in Collection)
                            {
                                if (reportItem.Code != report.Code) continue;
                                if (reportItem.Target is null) continue;

                                Element element = (Element)reportItem.Target;

                                if (!element.IsValidObject) continue;

                                try
                                {
                                    var feedback = _ignoreElementProvider.Ignore(report.Code, element);
                                    feedbackMessage.Append(feedback.Message);
                                    feedbackMessage.Append(Environment.NewLine);
                                    if (feedback.Result == IgnoreElementResult.Failed) hasErrors = true;
                                }
                                catch (Exception)
                                {
                                    hasErrors = true;
                                }
                            }
                            return !hasErrors;
                        });
                    var response = await _mediator.Send(command);

                    string message = response is { Result: false } or { HasError: true }
                        ? "Something went wrong while attempting to ignore the element with id: '{elementId}'. " 
                        + Environment.NewLine + $"Details: {feedbackMessage}"
                        : "The element with id: '{elementId}' has been successfully ignored.";
                    _fixReportSender.Send(new FixReport(
                        report.Code, report.Document.Title, new(message, ("elementId", elementId))));
                }
            };

            return _elementFixes
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
                            LambdaExternalEventTransactionCommand command = new(
                                doc, transactionName, async (_, _) => i.Execute(element));
                            var response = await _mediator.Send(command);

                            string message = response is { Result: false } or { HasError: true }
                                ? "Something went wrong while attempting to fix the element with id: '{elementId}'."
                                : "The element with id: '{elementId}' has been successfully corrected.";
                            _fixReportSender.Send(new FixReport(
                                i.Identity.Code, report.Document.Title, new(message, ("elementId", elementId))));
                        }
                    };
                    fixes.Add(fix);

                    iconColor = System.Windows.Media.Color.FromRgb(0xFF, 0xB7, 0x4D);
                    icon = new()
                    {
                        Kind = PackIconKind.Idea,
                        Foreground = new SolidColorBrush(iconColor)
                    };
                    FixViewModel fixAll = new() {
                        Icon = icon,
                        Title = $"{i.Value} (all)",
                        FixDelegate = async (cancellationToken) => {
                            if (doc is null or { IsValidObject: false }) return;

                            string transactionName = $"{i.Value} (all)";
                            LambdaExternalEventTransactionCommand command = new(
                                doc, transactionName, async (_, _) => {
                                    bool hasErrors = false;
                                    foreach (var reportItem in Collection)
                                    {
                                        if (reportItem.Code != report.Code) continue;
                                        if (reportItem.Target is null) continue;

                                        Element element = (Element)reportItem.Target;

                                        if (!element.IsValidObject) continue;

                                        try
                                        {
                                            bool result = i.Execute(element);
                                            if (!result) hasErrors = true;
                                        }
                                        catch (Exception)
                                        {
                                            hasErrors = true;
                                        }
                                    }
                                    return !hasErrors;
                                });
                            var response = await _mediator.Send(command);

                            string message = response is { Result: false } or { HasError: true }
                                ? "Something went wrong while attempting to fix the elements."
                                : "The elements has been successfully corrected.";
                            _fixReportSender.Send(new FixReport(i.Identity.Code, report.Document.Title, new(message)));
                        }
                    };
                    fixes.Add(fixAll);

                    return fixes;
                }).Append(ignoreFix).Append(ignoreFixAll).ToList();
        }
        else if (report.Target is Document document)
        {
            string documentTitle = document.Title;
            return _documentFixes
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
                            LambdaExternalEventTransactionCommand command = new(
                                document, transactionName, async (_, _) => i.Execute(document));
                            var response = await _mediator.Send(command);

                            string message = response is { Result: false } or { HasError: true }
                                ? "Something went wrong while attempting to fix the document with id: '{documentTitle}'."
                                : "The document with id: '{documentTitle}' has been successfully corrected.";
                            _fixReportSender.Send(new FixReport(
                                i.Identity.Code, report.Document.Title,
                                new(message, ("documentTitle", documentTitle))));
                        }
                    };
                    return fix;
                }).ToList();
        }
        return [];
    }
}
