using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Revit.Linter.ConfigurationPath.Abstractions.Models;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.DiagnosticListPresenter.Abstractions.Services;
using Revit.Linter.Localization;
using Revit.Linter.Presentation.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using System.Windows.Threading;

namespace Revit.Linter.DiagnosticListPresenter.ViewModels;

[XamlConstructor]
[AutoConstructor]
[GenerateLocalizedProperties]
internal sealed partial class DiagnosticListViewModel : InitializableObservableObject, IDiagnosticListFilterReset
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDiagnosticCatalog _diagnosticCatalog;
    private readonly IDocumentationLauncher _documentationLauncher;
    private readonly IUserInterfaceActivityStream _activityStream;
    private readonly IDialog _dialog;
    private readonly IConfirmationDialog _confirmationDialog;
    private readonly ILogger<DiagnosticListViewModel> _logger;
    private readonly IDiagnosticConfigurationWriter _configurationWriter;
    private readonly IConfigurationPaths _configurationPaths;
    private readonly IShellLauncher _shellLauncher;
    private FilteredRowCollection<DiagnosticItemViewModel>? _rows;
    private IDiagnosticCatalogSnapshotLease? _catalogLease;
    private bool _catalogChangesEnabled;
    private bool _isResettingFilters;
    private Dispatcher? _dispatcher;

    [ObservableProperty]
    public partial ObservableCollection<DiagnosticItemViewModel> Collection { get; private set; } = null!;
    partial void OnCollectionChanged(ObservableCollection<DiagnosticItemViewModel> value)
    {
        Rows.Attach(value);
        CollectionViewSource = Rows.ViewSource;
        RefreshFilters();
    }

    [ObservableProperty]
    public partial CollectionViewSource? CollectionViewSource { get; private set; }

    [ObservableProperty]
    public partial string SearchField { get; set; } = string.Empty;
    partial void OnSearchFieldChanged(string value)
    {
        // Search typing refreshes the list immediately; the tour observes the typed text
        // separately, so typing alone never completes a step that also asks for filter actions.
        Rows.Refresh();
        _activityStream.Publish(new DiagnosticSearchChangedActivity(value));
    }

    [ObservableProperty]
    public partial IEnumerable<IDiagnosticFilter<DiagnosticItemViewModel>> Filters { get; private set; } = [];
    partial void OnFiltersChanged(
        IEnumerable<IDiagnosticFilter<DiagnosticItemViewModel>> oldValue,
        IEnumerable<IDiagnosticFilter<DiagnosticItemViewModel>> newValue)
        => Rows.SetFilters("target-type", newValue);

    /// <summary>Clears the search box and re-enables every filter without publishing user activity.</summary>
    public void ResetSearchAndFilters()
    {
        SearchField = string.Empty;
        _isResettingFilters = true;
        try
        {
            foreach (DiagnosticTargetTypeFilterViewModel filter in Filters.OfType<DiagnosticTargetTypeFilterViewModel>())
                filter.IsActive = true;
        }
        finally
        {
            _isResettingFilters = false;
        }
    }

    #region [FilterByTag] Command - Filter by tag

    /// <summary>
    /// Puts a row tag into the search box, so the list shows the diagnostics that carry it.
    /// </summary>
    /// <param name="tag">The tag clicked in a row.</param>
    [RelayCommand]
    private void FilterByTag(string? tag)
    {
        if (tag is null || string.IsNullOrWhiteSpace(tag)) return;
        SearchField = tag;
    }

    #endregion

    #region [CheckAll] Command - Check all

    /// <summary> Check all </summary>
    private static readonly DocumentationPage PanePage = new("Diagnostics pane", "Панель диагностики");

    /// <summary>Opens the documentation of this pane; bound to F1 over the pane's help tooltips.</summary>
    [RelayCommand]
    private void OpenHelp() => _documentationLauncher.Open(PanePage);

    private static readonly DocumentationPage DiagnosticsOverviewPage = new("Diagnostics overview", "Обзор диагностики");

    /// <summary>
    /// Opens the documentation page of the module that registered the diagnostic.
    /// </summary>
    /// <remarks>
    /// The page is resolved when the user asks for it, not when the row is created. A code that is no longer
    /// in the catalog, for example after its rule was removed from a configuration file, opens the
    /// diagnostics overview instead.
    /// </remarks>
    private void OpenDocumentation(string code) => _documentationLauncher.Open(
        _catalogLease?.Snapshot.FindDocumentation(code) ?? DiagnosticsOverviewPage);

    [RelayCommand]
    private void CheckAll()
        => UpdateIsActive(Collection, _ => true);

    #endregion

    #region [OpenConfigurationFolder] Command - Open configuration folder

    /// <summary>Opens the configuration folder for the running Revit version.</summary>
    [RelayCommand]
    private async Task OpenConfigurationFolder(CancellationToken cancellationToken)
    {
        try
        {
            string directory = _configurationPaths.Directory;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // The shell launcher reports a refused launch itself; the command only records the completed
            // user action, so an opening failure does not also raise a second dialog here.
            if (_shellLauncher.OpenFolder(directory))
                _activityStream.Publish(new ConfigurationFolderOpenedActivity());
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to prepare the diagnostic configuration folder");
            await _dialog.Show(
                new DialogRequest(GetLocalizedString("configurationFolder_openFailed_message", exception.Message)),
                cancellationToken);
        }
    }

    #endregion

    #region [UncheckAll] Command - Uncheck all

    /// <summary> Uncheck all </summary>
    [RelayCommand]
    private void UncheckAll()
        => UpdateIsActive(Collection, _ => false);

    #endregion

    #region [InvertAll] Command - Invert all

    /// <summary> Invert all </summary>
    [RelayCommand]
    private void InvertAll()
        => UpdateIsActive(Collection, value => !value);

    #endregion

    #region [CheckVisible] Command - Check visible

    /// <summary> Check visible </summary>
    [RelayCommand]
    private void CheckVisible()
        => UpdateIsActive(CollectionViewSource!.View.Cast<DiagnosticItemViewModel>(), _ => true);

    #endregion

    #region [UncheckVisible] Command - Uncheck visible

    /// <summary> Uncheck visible </summary>
    [RelayCommand]
    private void UncheckVisible()
        => UpdateIsActive(CollectionViewSource!.View.Cast<DiagnosticItemViewModel>(), _ => false);

    #endregion

    #region [InvertVisible] Command - Invert visible

    /// <summary> Invert visible </summary>
    [RelayCommand]
    private void InvertVisible()
        => UpdateIsActive(CollectionViewSource!.View.Cast<DiagnosticItemViewModel>(), value => !value);

    #endregion

    #region [ResetSettings] Command - Reset settings

    /// <summary>Clears saved check settings and restores the values from the rules.</summary>
    /// <remarks>
    /// Clearing the persisted overrides falls back to the configuration defaults through the
    /// override subscriptions and publishes no selection activity, so an active practical tour
    /// cannot advance spuriously.
    /// </remarks>
    [RelayCommand]
    private async Task ResetSettings(CancellationToken cancellationToken)
    {
        bool confirmed = await _confirmationDialog.Show(
            new ConfirmationDialogRequest(
                GetLocalizedString("resetConfirm_message"), ResetSettingsText),
            cancellationToken);
        if (!confirmed) return;

        try
        {
            _configurationWriter.ResetBuiltInOverrides();
            _diagnosticCatalog.Refresh();
            _logger.LogInformation("Diagnostic settings were reset to their defaults");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to reset diagnostic settings");
            await _dialog.Show(
                new DialogRequest(GetLocalizedString("resetFailed_message", exception.Message)),
                cancellationToken);
        }
    }

    #endregion

    private void UpdateIsActive(
        IEnumerable<DiagnosticItemViewModel> source,
        Func<bool, bool> transform)
    {
        var items = source.ToArray();

        // Only genuine transitions are written and published. Each write persists the same state to the
        // configuration file the row came from, so a bulk action over hidden rules cannot occur: hidden
        // rows are never part of the collection.
        List<(DiagnosticItemViewModel Item, DiagnosticRuleStateUpdate Update)> changes = [];
        foreach (DiagnosticItemViewModel item in items)
        {
            bool before = item.IsActive;
            bool after = transform(before);
            if (after == before) continue;
            if (item.CreateStateUpdate(after) is { } update)
                changes.Add((item, update));
        }

        // One batch per configuration file: a bulk action rewrites each file once instead of once per
        // row, and the rows of a failed file keep their previous state.
        foreach (var file in changes.GroupBy(
                     change => change.Item.ConfigurationPath!,
                     StringComparer.OrdinalIgnoreCase))
        {
            DiagnosticRuleStateUpdate[] updates = file.Select(change => change.Update).ToArray();
            try
            {
                _configurationWriter.UpdateStates(file.Key, updates);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to persist the state of {DiagnosticCount} diagnostics to {ConfigurationPath}",
                    updates.Length, file.Key);
                continue;
            }

            foreach (var change in file)
                change.Item.ApplyPersistedState(change.Update.IsActive, change.Update.Severity);
        }
    }

    private FilteredRowCollection<DiagnosticItemViewModel> Rows => _rows ??=
        new(
            [
                new SortDescription(nameof(DiagnosticItemViewModel.IsTour), ListSortDirection.Descending),
                new SortDescription(nameof(DiagnosticItemViewModel.IsDebug), ListSortDirection.Descending),
                new SortDescription(nameof(DiagnosticItemViewModel.Code), ListSortDirection.Ascending),
            ],
            IsRowVisible,
            OnFilterToggled);

    private bool IsRowVisible(DiagnosticItemViewModel viewModel)
        => Filters.Any(filter => filter.IsActive && filter.IsValid(viewModel))
        && (viewModel.Description.ToString().Contains(SearchField, StringComparison.CurrentCultureIgnoreCase)
        || viewModel.Code.Contains(SearchField, StringComparison.CurrentCultureIgnoreCase)
        || DiagnosticTags.Matches(viewModel.Tags, SearchField));

    private void OnFilterToggled()
    {
        if (!_isResettingFilters)
            _activityStream.Publish(new DiagnosticListFilteredActivity());
    }

    private void RefreshFilters()
    {
        Filters = Enum.GetValues(typeof(TargetType))
            .Cast<TargetType>()
            .Select(targetType => new DiagnosticTargetTypeFilterViewModel(
                targetType,
                Collection.Count(item => item.TargetType == targetType)))
            .Where(filter => filter is DiagnosticTargetTypeFilterViewModel { Count: > 0 })
            .ToArray();
    }

    protected override async Task OnInitializing(CancellationToken cancellationToken = default)
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        await base.OnInitializing(cancellationToken);
        ReplaceCatalogSnapshot();
        _catalogChangesEnabled = true;
        _diagnosticCatalog.Changed += DiagnosticCatalog_Changed;
    }

    protected override async Task OnDeinitializing(CancellationToken cancellationToken = default)
    {
        _catalogChangesEnabled = false;
        _diagnosticCatalog.Changed -= DiagnosticCatalog_Changed;
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
        List<DiagnosticItemViewModel> items = [];
        IDiagnosticCatalogSnapshotLease lease = _diagnosticCatalog.AcquireSnapshot();
        try
        {
            DiagnosticCatalogSnapshot snapshot = lease.Snapshot;
            foreach (ElementDiagnosticRegistration registration in snapshot.ElementDiagnostics)
            {
                if (registration.State.IsHidden) continue;
                var viewModel = _serviceProvider.GetRequiredService<DiagnosticItemViewModel>();
                viewModel.Initialize(
                    registration,
                    () => OpenDocumentation(registration.Identity.Code),
                    PublishSelectionChanged);
                items.Add(viewModel);
            }
            foreach (DocumentDiagnosticRegistration registration in snapshot.DocumentDiagnostics)
            {
                if (registration.State.IsHidden) continue;
                var viewModel = _serviceProvider.GetRequiredService<DiagnosticItemViewModel>();
                viewModel.Initialize(
                    registration,
                    () => OpenDocumentation(registration.Identity.Code),
                    PublishSelectionChanged);
                items.Add(viewModel);
            }

            Collection = new(items);
            _catalogLease?.Dispose();
            _catalogLease = lease;
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    /// <summary>Publishes a selection change together with the complete set of enabled diagnostics.</summary>
    /// <remarks>
    /// The activity carries every enabled code so an observer can require one specific diagnostic to be the
    /// only enabled check, without counting the separate per-row events itself.
    /// </remarks>
    private void PublishSelectionChanged(DiagnosticItemViewModel changed)
    {
        string[] activeCodes = Collection
            .Where(item => item.IsActive)
            .Select(item => item.Code)
            .ToArray();
        _activityStream.Publish(new DiagnosticSelectionChangedActivity(
            changed.Code,
            changed.IsActive,
            activeCodes));
    }
}
