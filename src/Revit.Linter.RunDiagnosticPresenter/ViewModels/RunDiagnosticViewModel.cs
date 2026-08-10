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
using Revit.Linter.ValueStore.Abstractions.Services;
using System.Diagnostics;

namespace Revit.Linter.RunDiagnosticPresenter.ViewModels;

[XamlConstructor]
[GenerateLocalizedProperties]
internal sealed partial class RunDiagnosticViewModel : RevitInteractionViewModel
{
    private readonly IRevitContext _revitContext;
    private readonly IDiagnosticService _diagnosticService;
    private readonly IDiagnosticReportPresenter _diagnosticReportPresenter;
    private readonly IValueStore<RunDiagnosticSettings> _store;
    private readonly IDialog _dialog;
    private readonly IDisposable _changeSubscription;
    private bool _applyingExternalChanges;

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

    [ObservableProperty]
    public partial string DiagnosticTime { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial bool OnActiveViewMode { get; set; } = false;
    partial void OnOnActiveViewModeChanged(bool value)
    {
        if (_applyingExternalChanges) return;
        _store.Update(s => s.OnActiveViewMode = value);
    }

    #region [RunDiagnostic] Command - Запустить диагностику  

    [RelayCommand(CanExecute = nameof(CanRunDiagnostic))]
    private async Task RunDiagnostic(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        Document? targetDocument = _revitContext.ActiveDocument;
        if (targetDocument is null) return;

        View? targetView = OnActiveViewMode ? targetDocument.ActiveView : null;

        _diagnosticReportPresenter.Clear(targetDocument.Title);

        DiagnosticServiceResult diagnosticResult = _diagnosticService.Execute(targetDocument, targetView);

        _diagnosticReportPresenter.Refresh();

        DiagnosticTime = GetLocalizedString("diagnosticDuration_text", stopwatch.Elapsed.TotalSeconds);
        stopwatch.Stop();

        if (diagnosticResult == DiagnosticServiceResult.Failed)
            await _dialog.Show(new DialogRequest(GetLocalizedString("diagnosticsFailed_message")), cancellationToken);
    }

    private bool CanRunDiagnostic() => _revitContext.ActiveDocument is { IsFamilyDocument: false };

    #endregion

    protected async override Task OnInitializing(CancellationToken cancellationToken = default)
    {
        await base.OnInitializing(cancellationToken);

        RunDiagnosticCommand.NotifyCanExecuteChanged();
    }
    protected async override Task OnDeinitializing(CancellationToken cancellationToken = default)
    {
        _changeSubscription.Dispose();
        await base.OnDeinitializing(cancellationToken);
    }

    protected override void OnRevitChanged()
    {
        RunDiagnosticCommand.NotifyCanExecuteChanged();
    }
}
