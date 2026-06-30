using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Revit.Context.Abstractions.Services;
using Revit.Events.Abstractions.Services;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DiagnosticReportPresenter.Interactions;
using Revit.Linter.RunDiagnosticPresenter.ViewModels.Base;
using System.Diagnostics;

namespace Revit.Linter.RunDiagnosticPresenter.ViewModels;

[XamlConstructor]
internal sealed partial class RunDiagnosticViewModel : RevitInteractionViewModel
{
    private readonly IRevitContext _revitContext;
    private readonly IDiagnosticService _diagnosticService;
    private readonly IDiagnosticReportPresenter _diagnosticReportPresenter;

    public RunDiagnosticViewModel(
            IRevitContext revitContext, IAsyncExternalEvent externalEvent,
            IDiagnosticService diagnosticService, IDiagnosticReportPresenter diagnosticReportViewModel) : base(externalEvent)
    {
        _revitContext = revitContext;
        _diagnosticService = diagnosticService;
        _diagnosticReportPresenter = diagnosticReportViewModel;
    }

    [ObservableProperty]
    public partial string DiagnosticTime { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial bool OnActiveViewMode { get; set; } = false;

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
        _diagnosticReportPresenter.Clear();

        Document? targetDocument = _revitContext.ActiveDocument;
        if (targetDocument is null) return;

        View? targetView = OnActiveViewMode ? targetDocument.ActiveView : null;

        _diagnosticService.Excecute(targetDocument, targetView);

        _diagnosticReportPresenter.Refresh();
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
        await base.OnDeinitializing(cancellationToken);
    }

    protected override void OnRevitChanged()
    {
        RunDiagnosticCommand.NotifyCanExecuteChanged();
    }
}
