using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Revit.Context.Abstractions.Services;
using Revit.Linter.Diagnostic.Abstractions.Services;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DiagnosticReportPresenter.Interactions.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.Localization;
using Revit.Linter.RunDiagnosticPresenter.ViewModels.Base;
using Toolkit.ValueStore.Abstractions;
using System.Diagnostics;

namespace Revit.Linter.RunDiagnosticPresenter.ViewModels;

/// <summary>
/// Coordinates diagnostic execution, persisted scope settings, and report presentation for the current document.
/// </summary>
[XamlConstructor]
[GenerateLocalizedProperties]
public sealed partial class RunDiagnosticViewModel : RevitInteractionViewModel
{
    private readonly IRevitContext _revitContext;
    private readonly IDiagnosticService _diagnosticService;
    private readonly IDiagnosticReportPresenter _diagnosticReportPresenter;
    private readonly IValueStore<RunDiagnosticSettings> _store;
    private readonly IDialog _dialog;
    private readonly IDisposable _changeSubscription;
    private bool _applyingExternalChanges;

    /// <summary>
    /// Initializes the diagnostic-run view model and subscribes it to persisted setting changes.
    /// </summary>
    /// <param name="revitContext">Provides the current Revit document and application.</param>
    /// <param name="idlingScheduler">Schedules subscription work in a valid Revit API context.</param>
    /// <param name="diagnosticService">Executes registered diagnostics.</param>
    /// <param name="diagnosticReportViewModel">Presents and refreshes diagnostic results.</param>
    /// <param name="store">Persists diagnostic-run settings.</param>
    /// <param name="dialog">Displays diagnostic execution failures.</param>
    public RunDiagnosticViewModel(
            IRevitContext revitContext, IRevitIdlingScheduler idlingScheduler,
            IDiagnosticService diagnosticService,
            IDiagnosticReportPresenter diagnosticReportViewModel,
            IValueStore<RunDiagnosticSettings> store,
            IDialog dialog) : base(idlingScheduler)
    {
        _revitContext = revitContext;
        _diagnosticService = diagnosticService;
        _diagnosticReportPresenter = diagnosticReportViewModel;
        _store = store;
        _dialog = dialog;

        OnActiveViewMode = _store.CurrentValue.OnActiveViewMode;

        _changeSubscription = _store.OnChange(OnStoreValueChanged);
    }

    private void OnStoreValueChanged(RunDiagnosticSettings settings)
    {
        _applyingExternalChanges = true;
        OnActiveViewMode = settings.OnActiveViewMode;
        _applyingExternalChanges = false;
    }

    /// <summary>
    /// Gets the localized duration of the most recent diagnostic run.
    /// </summary>
    [ObservableProperty]
    public partial string DiagnosticTime { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether diagnostics are restricted to the active view.
    /// </summary>
    [ObservableProperty]
    public partial bool OnActiveViewMode { get; set; } = false;
    partial void OnOnActiveViewModeChanged(bool value)
    {
        if (_applyingExternalChanges) return;
        _store.Update(s => s.OnActiveViewMode = value);
    }

    #region [RunDiagnostic] Command - Run diagnostics

    [RelayCommand(CanExecute = nameof(CanRunDiagnostic))]
    private async Task RunDiagnostic(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        Document? targetDocument = _revitContext.ActiveDocument;
        if (targetDocument is null) return;

        bool onActiveView = OnActiveViewMode;
        DiagnosticServiceResult diagnosticResult = DiagnosticServiceResult.Success;

        // The command is raised by WPF, outside the Revit API context. Clearing the report restores an
        // active visualization, which needs a transaction, and Revit refuses to start one there. The whole
        // run therefore goes through the Idling scheduler: the visualization is restored first, so a run
        // limited to the active view sees the view as the user left it, and the diagnostics read the
        // model in a valid API context.
        await _idlingScheduler.RunAsync(_ =>
        {
            if (!targetDocument.IsValidObject) return;

            View? targetView = onActiveView ? targetDocument.ActiveView : null;

            _diagnosticReportPresenter.Clear(targetDocument.Title);

            diagnosticResult = _diagnosticService.Execute(targetDocument, targetView);

            _diagnosticReportPresenter.Refresh();
        }, cancellationToken);

        DiagnosticTime = GetLocalizedString("diagnosticDuration_text", stopwatch.Elapsed.TotalSeconds);
        stopwatch.Stop();

        if (diagnosticResult == DiagnosticServiceResult.Failed)
            await _dialog.Show(new DialogRequest(GetLocalizedString("diagnosticsFailed_message")), cancellationToken);
    }

    private bool CanRunDiagnostic() => _revitContext.ActiveDocument is { IsFamilyDocument: false };

    #endregion

    /// <inheritdoc />
    protected async override Task OnInitializing(CancellationToken cancellationToken = default)
    {
        await base.OnInitializing(cancellationToken);

        RunDiagnosticCommand.NotifyCanExecuteChanged();
    }
    /// <inheritdoc />
    protected async override Task OnDeinitializing(CancellationToken cancellationToken = default)
    {
        _changeSubscription.Dispose();
        await base.OnDeinitializing(cancellationToken);
    }

    /// <inheritdoc />
    protected override void OnRevitChanged()
    {
        RunDiagnosticCommand.NotifyCanExecuteChanged();
    }
}
