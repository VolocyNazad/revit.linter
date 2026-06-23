using Revit.Linter.Infrastructure.Utils;
using System.Windows.Controls;

namespace Revit.Linter;

public class DiagnosticListDockablePaneProvider(UserControl uiControl) : IDockablePaneProvider
{
    public void SetupDockablePane(DockablePaneProviderData data)
    {
        data.FrameworkElement = uiControl;
        data.InitialState = new()
        {
            DockPosition = DockPosition.Tabbed,
            TabBehind = DiagnosticReportPaneUtils.PaneId
        };
    }
}