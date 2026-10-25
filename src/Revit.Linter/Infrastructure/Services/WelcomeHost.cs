using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Revit.Context.Abstractions.Services;
using Revit.Linter.Core.Abstractions;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.DiagnosticListPresenter.Abstractions.Services;
using Revit.Linter.Infrastructure.Extensions;
using Revit.Linter.Infrastructure.Utils;
using Revit.Linter.WelcomePresenter.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Abstractions.Services;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
using System.IO;

namespace Revit.Linter.Infrastructure.Services;

/// <summary>
/// Gives the onboarding experiences the ribbon, pane, notification and link actions that only the add-in host owns.
/// </summary>
internal sealed class WelcomeHost(
    IRevitContext revitContext,
    IStringLocalizer<GlobalLocalizations> localizer,
    ILogger<WelcomeHost> logger,
    TutorialSampleOpenRequest tutorialSampleOpenRequest,
    ITutorialSampleCopyService tutorialSampleCopyService,
    IRevitIdlingScheduler idlingScheduler,
    IDialog dialog,
    IDiagnosticListFilterReset diagnosticListFilterReset,
    IConfigurationPaths paths,
    IShellLauncher shellLauncher) : IWelcomeHost, IPracticalTourHost
{
    private static string DocumentationUrl => ProductIdentity.DocumentationUrl;
    private static string SupportUrl => ProductIdentity.SupportUrl;

    private static readonly DocumentationPage QuickStartPage = new("Quick start", "Быстрый старт");

    public string RibbonTabName => localizer["ribbonTab_name"];

    public bool HasOpenDocument => revitContext.UIApplication?.ActiveUIDocument?.Document is not null;

    public void ShowPanes()
    {
        UIApplication? application = revitContext.UIApplication;
        if (application is null)
        {
            logger.LogWarning("The panes were not shown because the Revit application is unavailable");
            return;
        }

        try
        {
            foreach (DockablePaneId paneId in new[]
                     {
                         DiagnosticReportPaneUtils.PaneId, FixReportPaneUtils.PaneId, DiagnosticListPaneUtils.PaneId,
                         IgnoreListPaneUtils.PaneId,
                     })
            {
                DockablePane pane = application.GetDockablePane(paneId);
                if (!pane.IsShown()) pane.Show();
            }
        }
        catch (Exception exception)
        {
            // Showing the panes is a convenience of the wizard; the user can still open them from the ribbon.
            logger.LogError(exception, "Failed to show the panes after the welcome wizard");
        }
    }

    public void ShowPracticalTourPane()
        => QueuePracticalTourPaneVisibility(visible: true);

    public void ShowPane(WelcomePane pane)
        => _ = idlingScheduler.RunAsync(application =>
        {
            try
            {
                DockablePaneId paneId = GetPaneId(pane);
                application.GetDockablePane(paneId).Show();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to show the {WelcomePane} pane for onboarding", pane);
            }
        });

    private static DockablePaneId GetPaneId(WelcomePane pane)
        => pane switch
        {
            WelcomePane.Diagnostics => DiagnosticListPaneUtils.PaneId,
            WelcomePane.Warnings => DiagnosticReportPaneUtils.PaneId,
            WelcomePane.FixList => FixReportPaneUtils.PaneId,
            WelcomePane.IgnoreList => IgnoreListPaneUtils.PaneId,
            _ => throw new ArgumentOutOfRangeException(nameof(pane), pane, null),
        };

    public void HidePracticalTourPane()
        => QueuePracticalTourPaneVisibility(visible: false);

    public void ShowPracticalTourCompletedNotification()
        => _ = dialog.Show(new DialogRequest(localizer["practicalTour_completed_message"]));

    public void ResetDiagnosticsSearchAndFilters() => diagnosticListFilterReset.ResetSearchAndFilters();

    private void QueuePracticalTourPaneVisibility(bool visible)
        => _ = idlingScheduler.RunAsync(application => SetPracticalTourPaneVisibility(application, visible));

    private void SetPracticalTourPaneVisibility(UIApplication application, bool visible)
    {
        try
        {
            DockablePane pane = application.GetDockablePane(PracticalTourPaneUtils.PaneId);
            if (visible && !pane.IsShown()) pane.Show();
            if (!visible && pane.IsShown()) pane.Hide();
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException exception)
        {
            // The pane registers on first use: hiding it before that is a no-op, not a failure.
            // Note this is the Revit exception type, unrelated to System.ArgumentException.
            logger.LogDebug(exception, "Practical-tour pane is not created yet; nothing to hide");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to change practical-tour pane visibility to {Visible}", visible);
        }
    }

    public void QueueTutorialSample(string path)
    {
        string? replacedPath = tutorialSampleOpenRequest.Set(path);
        if (replacedPath is not null)
            tutorialSampleCopyService.DeleteCopy(replacedPath, paths.RevitVersion);
        InitExternalApplication.SetTutorialSampleButtonVisible(true);
        logger.LogInformation("Queued tutorial sample copy {TutorialSamplePath} for opening", path);
        _ = idlingScheduler.RunAsync(OpenQueuedTutorialSample);
    }

    private void OpenQueuedTutorialSample(UIApplication application)
    {
        if (!tutorialSampleOpenRequest.TryTake(out string? path) || path is null) return;

        try
        {
            application.OpenAndActivateDocument(path);
            tutorialSampleOpenRequest.TrackOpened(path);
            ShowPanes();
            InitExternalApplication.SetTutorialSampleButtonVisible(false);
            logger.LogInformation("Automatically opened tutorial sample copy {TutorialSamplePath}", path);
        }
        catch (Exception exception) when (exception is IOException or Autodesk.Revit.Exceptions.ApplicationException)
        {
            tutorialSampleOpenRequest.Set(path);
            InitExternalApplication.SetTutorialSampleButtonVisible(true);
            logger.LogWarning(
                exception,
                "Automatic opening of tutorial sample {TutorialSamplePath} failed; the ribbon command remains available",
                path);
        }
    }

    public void OpenQuickStart() => shellLauncher.OpenUrl(QuickStartPage.GetUrl());

    public void OpenDocumentation() => shellLauncher.OpenUrl(DocumentationUrl);

    public void OpenSupport() => shellLauncher.OpenUrl(SupportUrl);
}
