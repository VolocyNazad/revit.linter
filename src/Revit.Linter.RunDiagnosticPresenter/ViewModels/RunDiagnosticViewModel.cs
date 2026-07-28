using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Revit.Context.Abstractions.Services;
using Revit.Events.Abstractions.Services;
using Revit.Linter.Diagnostic.Abstractions.Services;
using Revit.Linter.DiagnosticReportPresenter.Interactions.Abstractions.Services;
using Revit.Linter.RunDiagnosticPresenter.ViewModels.Base;
using Revit.Linter.ValueStore.Abstractions.Services;
using Revit.Linter.WarningsHandling.Abstractions.Services;
using System.Diagnostics;

namespace Revit.Linter.RunDiagnosticPresenter.ViewModels;

[XamlConstructor]
internal sealed partial class RunDiagnosticViewModel : RevitInteractionViewModel
{
    private readonly IRevitContext _revitContext;
    private readonly IDiagnosticService _diagnosticService;
    private readonly IRevitWarningsService _revitWarningsService;
    private readonly IDiagnosticReportPresenter _diagnosticReportPresenter;
    private readonly IValueStore<RunDiagnosticSettings> _store;
    private readonly IDisposable _changeSubscription;
    private bool _applyingExternalChanges;

    public RunDiagnosticViewModel(
            IRevitContext revitContext, IAsyncExternalEvent externalEvent,
            IDiagnosticService diagnosticService, IRevitWarningsService revitWarningsService,
            IDiagnosticReportPresenter diagnosticReportViewModel,
            IValueStore<RunDiagnosticSettings> store) : base(externalEvent)
    {
        _revitContext = revitContext;
        _diagnosticService = diagnosticService;
        _revitWarningsService = revitWarningsService;
        _diagnosticReportPresenter = diagnosticReportViewModel;
        _store = store;

        OnActiveViewMode = _store.CurrentValue.OnActiveViewMode;
        IncludeRevitWarnings = _store.CurrentValue.IncludeRevitWarnings;

        _changeSubscription = _store.OnChange(OnStoreValueChanged);
    }

    private void OnStoreValueChanged(RunDiagnosticSettings settings)
    {
        _applyingExternalChanges = true;
        OnActiveViewMode = settings.OnActiveViewMode;
        IncludeRevitWarnings = settings.IncludeRevitWarnings;
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

    [ObservableProperty]
    public partial bool IncludeRevitWarnings { get; set; } = true;
    partial void OnIncludeRevitWarningsChanged(bool value)
    {
        if (_applyingExternalChanges) return;
        _store.Update(s => s.IncludeRevitWarnings = value);
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

        _diagnosticService.Execute(targetDocument, targetView);

        if (IncludeRevitWarnings)
            _revitWarningsService.Execute(targetDocument);

        _diagnosticReportPresenter.Refresh();

        DiagnosticTime = $"{stopwatch.Elapsed.Seconds} sec.";
        stopwatch.Stop();
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
