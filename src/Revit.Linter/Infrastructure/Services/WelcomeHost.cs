using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Revit.Context.Abstractions.Services;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Infrastructure.Extensions;
using Revit.Linter.Infrastructure.Utils;
using Revit.Linter.WelcomePresenter.Abstractions;

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

    public WelcomeHost(
        IRevitContext revitContext,
        IStringLocalizer<GlobalLocalizations> localizer,
        ILogger<WelcomeHost> logger)
    {
        _revitContext = revitContext;
        _localizer = localizer;
        _logger = logger;
    }

    public string RibbonTabName => _localizer["ribbonTab_name"];

    public string LogDirectory => ServiceCollectionExtensions.LogDirectory;

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

    public void OpenQuickStart() => ExternalPageLauncher.Open<WelcomeHost>(QuickStartPage.GetUrl());

    public void OpenDocumentation() => ExternalPageLauncher.Open<WelcomeHost>(DocumentationUrl);

    public void OpenSupport() => ExternalPageLauncher.Open<WelcomeHost>(SupportUrl);
}
