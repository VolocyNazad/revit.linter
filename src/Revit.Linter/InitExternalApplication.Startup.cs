// Queued startup notifications: the configuration migration warning, the welcome wizard and the hidden tour pane.

using Microsoft.Extensions.Logging;
using Revit.Linter.Infrastructure.Services;
using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions;
#if !AFTER2024
using Revit.Sugar;
#endif

namespace Revit.Linter;

internal sealed partial class InitExternalApplication
{
    /// <summary>
    /// Queues the configuration migration warning for the first Idling event.
    /// </summary>
    /// <remarks>
    /// The warning is shown only when the startup migration found legacy files and this product version has not
    /// warned yet. A failure is logged inside the warning service and never affects add-in startup.
    /// </remarks>
    private static void ScheduleConfigurationMigrationWarning(
        ConfigurationMigrationWarning warning, string productVersion) =>
        _ = GetService<RevitIdlingScheduler>().RunAsync(application =>
        {
            _ = application;
            _ = warning.ShowIfNeededAsync(productVersion);
        });

    /// <summary>
    /// Queues the first-run welcome wizard for the first Idling event, when the Revit main window exists.
    /// </summary>
    /// <remarks>
    /// A failure is logged and never affects add-in startup; the ribbon command remains available.
    /// Debug builds show every step on each start so the wizard can be worked on without resetting the
    /// stored state; release builds show only the steps the user has not seen.
    /// </remarks>
    private static void ScheduleWelcomeWizard() =>
        _ = GetService<RevitIdlingScheduler>().RunAsync(_ =>
        {
            try
            {
                var wizard = GetService<IWelcomeWizard>();
#if DEBUG
                wizard.Show();
#else
                wizard.ShowIfNeeded();
#endif
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to show the welcome wizard on startup");
            }
        });

    /// <summary>
    /// Hides the practical-tour pane that Revit may have restored visible from the previous session.
    /// </summary>
    /// <remarks>
    /// A fresh session has no tour view-model, so a restored pane would show no content (for example
    /// after the user refused the tour in the wizard or opted out of it). The pane is queued hidden
    /// before the wizard runs, so starting the tour afterwards shows it again. A failure is logged
    /// and never affects add-in startup.
    /// </remarks>
    private static void HidePracticalTourPaneWithoutSession()
    {
        try
        {
            GetService<IPracticalTour>().HidePaneIfInactive();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to hide the practical-tour pane on startup");
        }
    }
}

