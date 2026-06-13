using Autodesk.Revit.Attributes;
using Revit.Linter.Infrastructure.ExternalCommands;
using Revit.Linter.Infrastructure.Utils;

namespace Revit.Linter;

[Transaction(TransactionMode.Manual)]
public class ShowHideErrorListCommand : ExternalCommand
{
    public override void Execute()
    {
        DockablePane pane = Application.GetDockablePane(DiagnosticPaneUtils.PaneId);

        if (pane.IsShown()) pane.Hide();
        else pane.Show();
    }
}
