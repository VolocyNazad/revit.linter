using Microsoft.Extensions.Logging;
using Revit.Context.Abstractions.Services;
using Revit.Linter.Infrastructure.Utils;

namespace Revit.Linter.Infrastructure.Services;

/// <summary>Keeps the diagnostics panes open when Revit closes the last document and opens the next one.</summary>
/// <remarks>
/// Revit hides the add-in panes when the last document closes and does not show them again when the
/// next document opens. The visibility is remembered while a document is still open, when Revit is
/// about to close a document or open another one, and the panes that were open are shown once the
/// next document is ready. In zero-document state Revit keeps the panes hidden itself, so an
/// all-hidden state there is not remembered unless the user has opened the panes without a document.
/// The practical-tour pane is not part of this set: its visibility follows the tour session.
/// </remarks>
internal sealed class DiagnosticPanesVisibility(
    IRevitContext revitContext,
    ILogger<DiagnosticPanesVisibility> logger)
{
    private static readonly DockablePaneId[] Panes =
    [
        DiagnosticReportPaneUtils.PaneId,
        FixReportPaneUtils.PaneId,
        DiagnosticListPaneUtils.PaneId,
        IgnoreListPaneUtils.PaneId,
    ];

    private readonly DiagnosticPanesState _state = new();

    /// <summary>Remembers which diagnostics panes are open before Revit switches to another document.</summary>
    public void Remember()
    {
        UIApplication? application = revitContext.UIApplication;
        if (application is null) return;

        var shown = new bool[Panes.Length];
        for (int index = 0; index < Panes.Length; index++)
            shown[index] = IsShown(application, Panes[index]);

        _state.Remember(shown, application.ActiveUIDocument?.Document is not null);
    }

    /// <summary>Shows the diagnostics panes that were open before the document switch.</summary>
    public void Restore()
    {
        UIApplication? application = revitContext.UIApplication;
        IReadOnlyList<bool>? shown = _state.Shown;
        if (application is null || shown is null) return;

        int restoredCount = 0;
        int openCount = 0;
        for (int index = 0; index < Panes.Length; index++)
        {
            if (!shown[index]) continue;
            openCount++;

            try
            {
                DockablePane pane = application.GetDockablePane(Panes[index]);
                if (pane.IsShown()) continue;

                pane.Show();
                restoredCount++;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to restore a diagnostics pane after a document switch");
            }
        }

        if (openCount > 0)
            logger.LogInformation(
                "Diagnostics panes after a document switch: {RestoredCount} shown again, {OpenCount} were open before",
                restoredCount,
                openCount);
    }

    private static bool IsShown(UIApplication application, DockablePaneId paneId)
    {
        try
        {
            return DockablePane.PaneExists(paneId) && application.GetDockablePane(paneId).IsShown();
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException)
        {
            // A pane that Revit has not created yet counts as closed, like a hidden one.
            return false;
        }
    }
}
