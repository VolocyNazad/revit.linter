using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Revit.Context.Abstractions.Services;
using Revit.Linter.Core.Abstractions;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Infrastructure.Utils;
using Revit.Linter.WelcomePresenter.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions.Services;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
using System.IO;

namespace Revit.Linter.Infrastructure.Services;

/// <summary>
/// Gives the welcome wizard the ribbon, pane and link actions that only the add-in host owns.
/// </summary>
internal sealed class WelcomeHost(
    IRevitContext revitContext,
    IStringLocalizer<GlobalLocalizations> localizer,
    ILogger<WelcomeHost> logger,
    TutorialSampleOpenRequest tutorialSampleOpenRequest,
    ITutorialSampleCopyService tutorialSampleCopyService,
    IRevitIdlingScheduler idlingScheduler,
    IConfigurationPaths paths,
    IShellLauncher shellLauncher) : IWelcomeHost
{
    private static string DocumentationUrl => ProductIdentity.DocumentationUrl;
    private static string SupportUrl => ProductIdentity.SupportUrl;

    private static readonly DocumentationPage QuickStartPage = new("Quick start", "Быстрый старт");

    public string RibbonTabName => localizer["ribbonTab_name"];

    public bool HasOpenDocument => revitContext.UIApplication?.ActiveUIDocument?.Document is not null;

    public void ShowPanes() => OnboardingPanes.ShowAll(revitContext.UIApplication, logger);

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
