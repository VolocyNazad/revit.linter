using Autodesk.Revit.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Infrastructure.ExternalCommands;
using Revit.Linter.Infrastructure.Utils;

namespace Revit.Linter;

/// <summary>Toggles all Revit Linter dockable panes as a group.</summary>
[Transaction(TransactionMode.Manual)]
public class ShowAllPanesCommand : ExternalCommand
{
    /// <inheritdoc />
    public override void Execute()
    {
        DockablePane[] panes =
        [
            Application.GetDockablePane(DiagnosticReportPaneUtils.PaneId),
            Application.GetDockablePane(FixReportPaneUtils.PaneId),
            Application.GetDockablePane(DiagnosticListPaneUtils.PaneId),
            Application.GetDockablePane(IgnoreListPaneUtils.PaneId)
        ];

        DockablePane fixPane = Application.GetDockablePane(FixReportPaneUtils.PaneId);
        DockablePane ignorePane = Application.GetDockablePane(IgnoreListPaneUtils.PaneId);
        bool fixPaneWasShown = fixPane.IsShown();
        bool ignorePaneWasShown = ignorePane.IsShown();
        bool hidePanes = panes.All(pane => pane.IsShown());
        foreach (DockablePane pane in panes)
        {
            if (hidePanes) pane.Hide();
            else if (!pane.IsShown()) pane.Show();
        }

        if (!fixPaneWasShown && fixPane.IsShown())
            Program.Provider.GetRequiredService<IUserInterfaceActivityStream>()
                .Publish(new FixListPaneShownActivity());
        if (!ignorePaneWasShown && ignorePane.IsShown())
            Program.Provider.GetRequiredService<IUserInterfaceActivityStream>()
                .Publish(new IgnoreListPaneShownActivity());
    }
}
