using Microsoft.Extensions.Logging;
using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions.Models;

namespace Revit.Linter.Infrastructure.Utils;

/// <summary>
/// Shows the Revit Linter panes that the onboarding experiences bring to the foreground.
/// </summary>
internal static class OnboardingPanes
{
    /// <summary>Shows every add-in dockable pane that is currently hidden.</summary>
    /// <remarks>Requires a valid Revit API context; a missing application is logged and ignored.</remarks>
    internal static void ShowAll(UIApplication? application, ILogger logger)
    {
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
            // Showing the panes is a convenience of onboarding; the user can still open them from the ribbon.
            logger.LogError(exception, "Failed to show the onboarding panes");
        }
    }

    /// <summary>Gets the dockable pane identifier of an onboarding pane.</summary>
    internal static DockablePaneId GetId(OnboardingPane pane)
        => pane switch
        {
            OnboardingPane.Diagnostics => DiagnosticListPaneUtils.PaneId,
            OnboardingPane.Warnings => DiagnosticReportPaneUtils.PaneId,
            OnboardingPane.FixList => FixReportPaneUtils.PaneId,
            OnboardingPane.IgnoreList => IgnoreListPaneUtils.PaneId,
            _ => throw new ArgumentOutOfRangeException(nameof(pane), pane, null),
        };
}
