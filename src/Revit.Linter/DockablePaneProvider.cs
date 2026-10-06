namespace Revit.Linter;

/// <summary>Hosts a view in a dockable pane.</summary>
/// <param name="view">The pane content.</param>
/// <param name="tabBehind">
/// The pane behind which this one is opened as a tab; <see langword="null"/> uses <paramref name="dockPosition"/> instead.
/// </param>
/// <param name="dockPosition">The initial dock position used when the pane is not tabbed.</param>
internal sealed class DockablePaneProvider(
    System.Windows.FrameworkElement view,
    DockablePaneId? tabBehind = null,
    DockPosition dockPosition = DockPosition.Bottom)
    : IDockablePaneProvider
{
    public void SetupDockablePane(DockablePaneProviderData data)
    {
        data.FrameworkElement = view;
        data.InitialState = tabBehind is null
            ? new() { DockPosition = dockPosition }
            : new() { DockPosition = DockPosition.Tabbed, TabBehind = tabBehind };
    }
}
