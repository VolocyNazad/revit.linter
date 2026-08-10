using Autodesk.Revit.Attributes;
using Revit.Linter.Infrastructure.ExternalCommands;
using Revit.Linter.Infrastructure.Utils;

namespace Revit.Linter;

[Transaction(TransactionMode.Manual)]
public class ShowAllPanesCommand : ExternalCommand
{
    public override void Execute()
    {
        DockablePane[] panes =
        [
            Application.GetDockablePane(DiagnosticReportPaneUtils.PaneId),
            Application.GetDockablePane(FixReportPaneUtils.PaneId),
            Application.GetDockablePane(DiagnosticListPaneUtils.PaneId)
        ];

        bool hidePanes = panes.All(pane => pane.IsShown());
        foreach (DockablePane pane in panes)
        {
            if (hidePanes) pane.Hide();
            else if (!pane.IsShown()) pane.Show();
        }
    }
}
