using Autodesk.Revit.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Infrastructure.ExternalCommands;
using Revit.Linter.Infrastructure.Utils;

namespace Revit.Linter;

/// <summary>Toggles the fix report dockable pane.</summary>
[Transaction(TransactionMode.Manual)]
public class ShowHideFixListCommand : ExternalCommand
{
    /// <inheritdoc />
    public override void Execute()
    {
        DockablePane pane = Application.GetDockablePane(FixReportPaneUtils.PaneId);

        if (pane.IsShown()) pane.Hide();
        else
        {
            pane.Show();
            Program.Provider.GetRequiredService<IUserInterfaceActivityStream>()
                .Publish(new FixListPaneShownActivity());
        }
    }
}
