using Autodesk.Revit.Attributes;
using Revit.Linter.Infrastructure.ExternalCommands;
using Revit.Linter.Infrastructure.Utils;

namespace Revit.Linter;

/// <summary>Toggles the diagnostic report dockable pane.</summary>
[Transaction(TransactionMode.Manual)]
public class ShowHideErrorListCommand : ExternalCommand
{
    /// <inheritdoc />
    public override void Execute()
    {
        DockablePane pane = Application.GetDockablePane(DiagnosticReportPaneUtils.PaneId);

        if (pane.IsShown()) pane.Hide();
        else pane.Show();
    }
}
