using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Revit.Context.Abstractions.Services;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.Infrastructure.Extensions;
using Revit.Linter.Infrastructure.Utils;
using Revit.Linter.WelcomePresenter.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Abstractions.Services;
using Revit.Linter.ConfigurationPath;
using System.IO;

namespace Revit.Linter.Infrastructure.Services;

/// <summary>
/// Gives the welcome wizard the ribbon, log and pane facts that only the add-in host owns.
/// </summary>
internal sealed class WelcomeHost : IWelcomeHost
{
#pragma warning disable S1075 // These are the intentional, stable documentation and support destinations.
    private const string DocumentationUrl = "https://github.com/VolocyNazad/revit.linter/wiki";
    private const string SupportUrl = "https://github.com/VolocyNazad/revit.linter/issues";
#pragma warning restore S1075

    private static readonly DocumentationPage QuickStartPage = new("Quick start", "Быстрый старт");

    private readonly IRevitContext _revitContext;
    private readonly IStringLocalizer<GlobalLocalizations> _localizer;
    private readonly ILogger<WelcomeHost> _logger;
    private readonly TutorialSampleOpenRequest _tutorialSampleOpenRequest;
    private readonly ITutorialSampleCopyService _tutorialSampleCopyService;
    private readonly IRevitIdlingScheduler _idlingScheduler;
    private readonly IDialog _dialog;

    public WelcomeHost(
        IRevitContext revitContext,
        IStringLocalizer<GlobalLocalizations> localizer,
        ILogger<WelcomeHost> logger,
        TutorialSampleOpenRequest tutorialSampleOpenRequest,
        ITutorialSampleCopyService tutorialSampleCopyService,
        IRevitIdlingScheduler idlingScheduler,
        IDialog dialog)
    {
        _revitContext = revitContext;
        _localizer = localizer;
        _logger = logger;
        _tutorialSampleOpenRequest = tutorialSampleOpenRequest;
        _tutorialSampleCopyService = tutorialSampleCopyService;
        _idlingScheduler = idlingScheduler;
        _dialog = dialog;
    }

    public string RibbonTabName => _localizer["ribbonTab_name"];

    public string LogDirectory => ServiceCollectionExtensions.LogDirectory;

    public bool HasOpenDocument => _revitContext.UIApplication?.ActiveUIDocument?.Document is not null;

    public void ShowPanes()
    {
        UIApplication? application = _revitContext.UIApplication;
        if (application is null)
        {
            _logger.LogWarning("The panes were not shown because the Revit application is unavailable");
            return;
        }

        try
        {
            foreach (DockablePaneId paneId in new[]
                     {
                         DiagnosticReportPaneUtils.PaneId, FixReportPaneUtils.PaneId, DiagnosticListPaneUtils.PaneId,
                     })
            {
                DockablePane pane = application.GetDockablePane(paneId);
                if (!pane.IsShown()) pane.Show();
            }
        }
        catch (Exception exception)
        {
            // Showing the panes is a convenience of the wizard; the user can still open them from the ribbon.
            _logger.LogError(exception, "Failed to show the panes after the welcome wizard");
        }
    }

    public void ShowPracticalTourPane()
        => QueuePracticalTourPaneVisibility(visible: true);

    public void ShowPane(WelcomePane pane)
        => _ = _idlingScheduler.RunAsync(application =>
        {
            try
            {
                DockablePaneId paneId = GetPaneId(pane);
                application.GetDockablePane(paneId).Show();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to show the {WelcomePane} pane for onboarding", pane);
            }
        });

    private static DockablePaneId GetPaneId(WelcomePane pane)
        => pane switch
        {
            WelcomePane.Diagnostics => DiagnosticListPaneUtils.PaneId,
            WelcomePane.Warnings => DiagnosticReportPaneUtils.PaneId,
            WelcomePane.FixList => FixReportPaneUtils.PaneId,
            _ => throw new ArgumentOutOfRangeException(nameof(pane), pane, null),
        };

    public void HidePracticalTourPane()
        => QueuePracticalTourPaneVisibility(visible: false);

    public void ShowPracticalTourCompletedNotification()
        => _ = _dialog.Show(new DialogRequest(_localizer["practicalTour_completed_message"]));

    private void QueuePracticalTourPaneVisibility(bool visible)
        => _ = _idlingScheduler.RunAsync(application => SetPracticalTourPaneVisibility(application, visible));

    private void SetPracticalTourPaneVisibility(UIApplication application, bool visible)
    {
        try
        {
            DockablePane pane = application.GetDockablePane(PracticalTourPaneUtils.PaneId);
            if (visible && !pane.IsShown()) pane.Show();
            if (!visible && pane.IsShown()) pane.Hide();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to change practical-tour pane visibility to {Visible}", visible);
        }
    }

    public void QueueTutorialSample(string path)
    {
        string? replacedPath = _tutorialSampleOpenRequest.Set(path);
        if (replacedPath is not null)
            _tutorialSampleCopyService.DeleteCopy(replacedPath, ConfigurationPathUtils.RevitVersion);
        InitExternalApplication.SetTutorialSampleButtonVisible(true);
        _logger.LogInformation("Queued tutorial sample copy {TutorialSamplePath} for opening", path);
        _ = _idlingScheduler.RunAsync(OpenQueuedTutorialSample);
    }

    private void OpenQueuedTutorialSample(UIApplication application)
    {
        if (!_tutorialSampleOpenRequest.TryTake(out string? path) || path is null) return;

        try
        {
            application.OpenAndActivateDocument(path);
            _tutorialSampleOpenRequest.TrackOpened(path);
            ShowPanes();
            InitExternalApplication.SetTutorialSampleButtonVisible(false);
            _logger.LogInformation("Automatically opened tutorial sample copy {TutorialSamplePath}", path);
        }
        catch (Exception exception) when (exception is IOException or Autodesk.Revit.Exceptions.ApplicationException)
        {
            _tutorialSampleOpenRequest.Set(path);
            InitExternalApplication.SetTutorialSampleButtonVisible(true);
            _logger.LogWarning(
                exception,
                "Automatic opening of tutorial sample {TutorialSamplePath} failed; the ribbon command remains available",
                path);
        }
    }

    public void OpenQuickStart() => ExternalPageLauncher.Open<WelcomeHost>(QuickStartPage.GetUrl());

    public void OpenDocumentation() => ExternalPageLauncher.Open<WelcomeHost>(DocumentationUrl);

    public void OpenSupport() => ExternalPageLauncher.Open<WelcomeHost>(SupportUrl);
}
