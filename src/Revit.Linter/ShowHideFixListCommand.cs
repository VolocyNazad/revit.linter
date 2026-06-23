using Autodesk.Revit.Attributes;
using Revit.Linter.Infrastructure.ExternalCommands;
using Revit.Linter.Infrastructure.Utils;

namespace Revit.Linter;

[Transaction(TransactionMode.Manual)]
public class ShowHideFixListCommand : ExternalCommand
{
    public override void Execute()
    {
        DockablePane pane = Application.GetDockablePane(FixReportPaneUtils.PaneId);

        if (pane.IsShown()) pane.Hide();
        else pane.Show();
    }
}
