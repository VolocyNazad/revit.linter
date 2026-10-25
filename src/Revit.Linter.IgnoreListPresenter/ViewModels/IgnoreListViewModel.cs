using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Revit.Async;
using Revit.Context.Abstractions.Services;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementIgnoring.Abstractions.Models;
using Revit.Linter.ElementIgnoring.Abstractions.Services;
using Revit.Linter.Localization;
using Revit.Linter.Presentation.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Threading;

namespace Revit.Linter.IgnoreListPresenter.ViewModels;

[GenerateLocalizedProperties]
internal sealed partial class IgnoreListViewModel : RevitInteractionViewModel
{
    private readonly IRevitContext _revitContext;
    private readonly IIgnoreListManager _ignoreListManager;
    private readonly IEnumerable<IAccentElementsService> _accentElementsServices;
    private readonly IDialog _dialog;
    private readonly IConfirmationDialog _confirmationDialog;
    private readonly IDocumentationLauncher _documentationLauncher;
    private readonly IUserInterfaceActivityStream _activityStream;
    private readonly ILogger<IgnoreListViewModel> _logger;
    private FilteredRowCollection<IgnoreListItemViewModel>? _rows;
    private Dispatcher? _dispatcher;

    private static readonly DocumentationPage PanePage = new("Ignoring pane", "Панель игнорирования");

    public IgnoreListViewModel(
        IRevitContext revitContext,
        IRevitEventSource revitEventSource,
        IIgnoreListManager ignoreListManager,
        IEnumerable<IAccentElementsService> accentElementsServices,
        IDialog dialog,
        IConfirmationDialog confirmationDialog,
        IDocumentationLauncher documentationLauncher,
        IUserInterfaceActivityStream activityStream,
        ILogger<IgnoreListViewModel> logger)
        : base(revitEventSource)
    {
        _revitContext = revitContext;
        _ignoreListManager = ignoreListManager;
        _accentElementsServices = accentElementsServices;
        _dialog = dialog;
        _confirmationDialog = confirmationDialog;
        _documentationLauncher = documentationLauncher;
        _activityStream = activityStream;
        _logger = logger;

        Collection = [];
    }

    [ObservableProperty]
    public partial ObservableCollection<IgnoreListItemViewModel> Collection { get; private set; }
    partial void OnCollectionChanged(ObservableCollection<IgnoreListItemViewModel> value)
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
    public partial string? ActiveDocumentTitle { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowSelectedCommand))]
    public partial IgnoreListItemViewModel? SelectedEntry { get; set; }
    partial void OnSelectedEntryChanged(IgnoreListItemViewModel? value)
    {
        if (value is not null) _activityStream.Publish(new IgnoreSelectedActivity(value.Code));
    }

    /// <summary>Opens the documentation of this pane; bound to F1 over the pane's help tooltips.</summary>
    [RelayCommand]
    private void OpenHelp() => _documentationLauncher.Open(PanePage);

    /// <summary>Reloads the ignore list of the active document.</summary>
    [RelayCommand]
    private async Task Refresh() => await ReloadAsync(purgeMissing: true);

    /// <summary>Shows the target of an entry in the model.</summary>
    /// <param name="parameter">The entry to show, or <see langword="null"/> for the selected one.</param>
    [RelayCommand(CanExecute = nameof(CanShowSelected))]
    private void ShowSelected(object? parameter)
    {
        IgnoreListItemViewModel? row = parameter as IgnoreListItemViewModel ?? SelectedEntry;
        if (row is not { HasLiveElement: true, ElementIdValue: long elementIdValue }) return;
        Document? document = _revitContext.ActiveDocument;
        if (document is null) return;
        _accentElementsServices.SelectElement(document, ToElementId(elementIdValue));
    }

    private bool CanShowSelected(object? parameter) =>
        (parameter as IgnoreListItemViewModel ?? SelectedEntry) is { HasLiveElement: true }
        && _revitContext.ActiveDocument is { IsFamilyDocument: false };

    /// <summary>Stops ignoring an entry.</summary>
    /// <param name="parameter">The entry to stop ignoring, or <see langword="null"/> for the selected one.</param>
    /// <remarks>
    /// Entries whose elements are already missing cannot be addressed by an element object;
    /// they are cleaned automatically instead.
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanRemoveSelected))]
    private async Task RemoveSelected(object? parameter)
    {
        IgnoreListItemViewModel? row = parameter as IgnoreListItemViewModel ?? SelectedEntry;
        if (row is not { IsMissing: false }) return;
        Document? document = _revitContext.ActiveDocument;
        if (document is not { IsValidObject: true }) return;

        bool removed = await RevitTask.RunAsync(_ =>
        {
            using Transaction transaction = new(document, RemoveEntriesTransaction);
            transaction.Start();
            bool result = row.ElementIdValue is null
                ? _ignoreListManager.RemoveDocument(row.Code, document)
                : RemoveElementEntry(document, row);
            if (!result)
            {
                transaction.RollBack();
                return false;
            }
            return transaction.Commit() == TransactionStatus.Committed;
        });

        if (!removed)
            await _dialog.Show(new DialogRequest(OperationFailedMessage));
        await ReloadAsync();
    }

    private bool CanRemoveSelected(object? parameter) =>
        (parameter as IgnoreListItemViewModel ?? SelectedEntry) is { IsMissing: false };

    /// <summary>Removes every ignore entry of the active document after confirmation.</summary>
    [RelayCommand(CanExecute = nameof(CanClearAll))]
    private async Task ClearAll()
    {
        Document? document = _revitContext.ActiveDocument;
        if (document is not { IsValidObject: true }) return;
        string documentTitle = document.Title;

        bool confirmed = await _confirmationDialog.Show(
            new ConfirmationDialogRequest(
                GetLocalizedString("clearConfirm_message", documentTitle), ClearAllText));
        if (!confirmed) return;

        bool cleared = await RevitTask.RunAsync(_ =>
        {
            using Transaction transaction = new(document, ClearEntriesTransaction);
            transaction.Start();
            _ignoreListManager.Clear(document);
            return transaction.Commit() == TransactionStatus.Committed;
        });

        if (!cleared)
            await _dialog.Show(new DialogRequest(OperationFailedMessage));
        await ReloadAsync();
    }

    private bool CanClearAll() => Collection.Count > 0;

    protected override async Task OnInitializing(CancellationToken cancellationToken = default)
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        await base.OnInitializing(cancellationToken);
        await ReloadAsync(purgeMissing: true);
    }

    protected override async Task OnDeinitializing(CancellationToken cancellationToken = default)
    {
        _dispatcher = null;
        await base.OnDeinitializing(cancellationToken);
    }

    protected override void OnRevitChanged(RevitEventType revitEventType)
    {
        // Own mutations reload explicitly; every other document change may still affect the list
        // (undo, external edits, another pane), so the list is reloaded either way. Missing entries
        // are purged on settled loads only, never inside a change storm an undo may still revert.
        _ = ReloadAsync(purgeMissing: revitEventType != RevitEventType.DocumentChanged);
    }

    private FilteredRowCollection<IgnoreListItemViewModel> Rows => _rows ??=
        new(
            [new SortDescription(nameof(IgnoreListItemViewModel.Code), ListSortDirection.Ascending)],
            IsRowVisible);

    private bool IsRowVisible(IgnoreListItemViewModel viewModel)
        => viewModel.TargetText.Contains(SearchField, StringComparison.CurrentCultureIgnoreCase)
        || viewModel.Code.Contains(SearchField, StringComparison.CurrentCultureIgnoreCase);

    private async Task ReloadAsync(bool purgeMissing = false)
    {
        try
        {
            Document? document = _revitContext.ActiveDocument;
            if (document is not { IsValidObject: true })
            {
                await ShowRowsAsync([], null);
                return;
            }

            string documentTitle = document.Title;
            List<IgnoreListItemViewModel> rows = await RevitTask.RunAsync(_ => LoadRows(document));
            if (purgeMissing && rows.Any(row => row.IsMissing))
            {
                // Missing entries are purged on settled loads (open, switch, manual refresh),
                // never inside a change storm: an element restored by undo keeps its identifier.
                await RevitTask.RunAsync(_ =>
                {
                    using Transaction transaction = new(document, RemoveEntriesTransaction);
                    transaction.Start();
                    _ignoreListManager.RemoveMissing(document);
                    transaction.Commit();
                });
                rows = await RevitTask.RunAsync(_ => LoadRows(document));
            }
            _logger.LogDebug(
                "Reloaded {EntryCount} ignore entries of {DocumentTitle}",
                rows.Count, documentTitle);
            await ShowRowsAsync(rows, documentTitle);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to reload the ignore list");
        }
    }

    // Element names are read here, on the Revit thread; the rows handed to the view are plain data.
    private List<IgnoreListItemViewModel> LoadRows(Document document)
    {
        string documentTitle = document.Title;
        List<IgnoreListItemViewModel> rows = [];
        foreach (IgnoreListEntry entry in _ignoreListManager.GetEntries(document))
        {
            if (entry.ElementIdValue is null)
            {
                rows.Add(new IgnoreListItemViewModel
                {
                    Code = entry.Code,
                    ScopeText = ScopeDocumentText,
                    TargetText = documentTitle,
                    DocumentTitle = documentTitle,
                });
                continue;
            }

            Element? element = null;
            try
            {
                element = document.GetElement(ToElementId(entry.ElementIdValue.Value));
            }
            catch (Autodesk.Revit.Exceptions.InvalidObjectException)
            {
                // The element disappeared between the entry lookup and this read: the row is
                // shown as missing until an automatic cleanup removes it.
            }

            if (element is not { IsValidObject: true })
            {
                rows.Add(new IgnoreListItemViewModel
                {
                    Code = entry.Code,
                    ScopeText = ScopeElementText,
                    TargetText = DeletedTargetText,
                    IsMissing = true,
                    DocumentTitle = documentTitle,
                });
                continue;
            }

            rows.Add(new IgnoreListItemViewModel
            {
                Code = entry.Code,
                ScopeText = ScopeElementText,
                TargetText = element.Name,
                HasLiveElement = true,
                ElementIdValue = entry.ElementIdValue,
                DocumentTitle = documentTitle,
            });
        }
        return rows;
    }

    private bool RemoveElementEntry(Document document, IgnoreListItemViewModel row)
    {
        if (row.ElementIdValue is not long elementIdValue) return false;
        Element? element;
        try
        {
            element = document.GetElement(ToElementId(elementIdValue));
        }
        catch (Autodesk.Revit.Exceptions.InvalidObjectException)
        {
            return false;
        }
        if (element is not { IsValidObject: true }) return false;
        return _ignoreListManager.Remove(row.Code, element);
    }

    private Task ShowRowsAsync(List<IgnoreListItemViewModel> rows, string? documentTitle)
    {
        Dispatcher? dispatcher = _dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted)
        {
            ApplyRows(rows, documentTitle);
            return Task.CompletedTask;
        }
        return dispatcher.InvokeAsync(() => ApplyRows(rows, documentTitle)).Task;
    }

    private void ApplyRows(List<IgnoreListItemViewModel> rows, string? documentTitle)
    {
        Collection = new ObservableCollection<IgnoreListItemViewModel>(rows);
        ActiveDocumentTitle = documentTitle;
        ClearAllCommand.NotifyCanExecuteChanged();
    }

    private static ElementId ToElementId(long value) =>
#if BEFORE2024
        new ElementId((int)value);
#else
        new ElementId(value);
#endif
}
