using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Revit.Context.Abstractions.Services;
using Revit.Events.Abstractions.Services;
using Revit.Linter.Diagnostic.Abstractions.Services;
using Revit.Linter.DiagnosticReportPresenter.Interactions.Abstractions.Services;
using Revit.Linter.RunDiagnosticPresenter.ViewModels.Base;
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

    public RunDiagnosticViewModel(
            IRevitContext revitContext, IAsyncExternalEvent externalEvent,
            IDiagnosticService diagnosticService, IRevitWarningsService revitWarningsService, IDiagnosticReportPresenter diagnosticReportViewModel) : base(externalEvent)
    {
        _revitContext = revitContext;
        _diagnosticService = diagnosticService;
        _revitWarningsService = revitWarningsService;
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

        Document? targetDocument = _revitContext.ActiveDocument;
        if (targetDocument is null) return;

        View? targetView = OnActiveViewMode ? targetDocument.ActiveView : null;

        _diagnosticReportPresenter.Clear(targetDocument.Title);

        _diagnosticService.Execute(targetDocument, targetView);
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
        await base.OnDeinitializing(cancellationToken);
    }

    protected override void OnRevitChanged()
    {
        RunDiagnosticCommand.NotifyCanExecuteChanged();
    }
}
