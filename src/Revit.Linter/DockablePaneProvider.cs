namespace Revit.Linter;

/// <summary>Hosts a view in a dockable pane.</summary>
/// <param name="view">The pane content.</param>
/// <param name="tabBehind">
/// The pane behind which this one is opened as a tab; <see langword="null"/> docks it at the bottom instead.
/// </param>
internal sealed class DockablePaneProvider(System.Windows.FrameworkElement view, DockablePaneId? tabBehind = null)
    : IDockablePaneProvider
{
    public void SetupDockablePane(DockablePaneProviderData data)
    {
        data.FrameworkElement = view;
        data.InitialState = tabBehind is null
            ? new() { DockPosition = DockPosition.Bottom }
            : new() { DockPosition = DockPosition.Tabbed, TabBehind = tabBehind };
    }
}
