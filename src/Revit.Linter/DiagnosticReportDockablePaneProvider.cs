using System.Windows.Controls;

namespace Revit.Linter;

internal sealed class DiagnosticReportDockablePaneProvider(UserControl uiControl) : IDockablePaneProvider
{
    public void SetupDockablePane(DockablePaneProviderData data)
    {
        data.FrameworkElement = uiControl;
        data.InitialState = new()
        {
            DockPosition = DockPosition.Bottom,
        };
    }
}
