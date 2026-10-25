using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Revit.Context.Abstractions.Services;
using Revit.Linter.Core.Abstractions;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DiagnosticListPresenter.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.Infrastructure.Utils;
using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions;
using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions.Models;

namespace Revit.Linter.Infrastructure.Services;

/// <summary>
/// Gives the practical tour the pane, notification and diagnostics-filter actions that only the add-in host owns.
/// </summary>
internal sealed class PracticalTourHost(
    IRevitContext revitContext,
    ILogger<PracticalTourHost> logger,
    IRevitIdlingScheduler idlingScheduler,
    IDialog dialog,
    IDiagnosticListFilterReset diagnosticListFilterReset,
    IStringLocalizer<GlobalLocalizations> localizer) : IPracticalTourHost
{
    public bool HasOpenDocument => revitContext.UIApplication?.ActiveUIDocument?.Document is not null;

    public void ShowPanes() => OnboardingPanes.ShowAll(revitContext.UIApplication, logger);

    public void ShowPane(OnboardingPane pane)
        => _ = idlingScheduler.RunAsync(application =>
        {
            try
            {
                application.GetDockablePane(OnboardingPanes.GetId(pane)).Show();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to show the {OnboardingPane} pane for onboarding", pane);
            }
        });

    public void ShowPracticalTourPane() => QueuePracticalTourPaneVisibility(visible: true);

    public void HidePracticalTourPane() => QueuePracticalTourPaneVisibility(visible: false);

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
}
