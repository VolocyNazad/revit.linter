using Revit.Linter.StatusBar.Infrasructure.Extensions;
using System.Windows;
using System.Windows.Controls;
using UIFramework;

namespace Revit.Linter.StatusBar.Infrasructure.Utils;

/// <summary>
/// StatusBarController
/// </summary>
internal static class StatusBarController
{
    private static readonly Grid RootGrid = MainWindow.getMainWnd().FindChild<Grid>("rootGrid") 
        ?? throw new InvalidOperationException("Cannot find root grid in Revit UI");
    private static readonly DialogBarControl InternalControl = RootGrid.FindChild<DialogBarControl>("statusBar")
            ?? throw new InvalidOperationException("Cannot find internal control in Revit UI");
    private static ContentPresenter? _controlPresenter;

    /// <summary>
    /// Default StatusBar is visible
    /// </summary>
    public static bool IsVisible => InternalControl.Visibility == Visibility.Visible;

    /// <summary>
    /// Show
    /// </summary>
    /// <param name="content"></param>
    public static void Show(FrameworkElement content)
    {
        InternalControl.Visibility = Visibility.Hidden;

        if (_controlPresenter is null)
        {
            _controlPresenter = new ContentPresenter();
            RootGrid.Children.Add(_controlPresenter);
        }

        _controlPresenter.Content = content;
        Grid.SetRow(_controlPresenter, Grid.GetRow(InternalControl));
    }

    /// <summary>
    /// Hide
    /// </summary>
    public static void Hide()
    {
        InternalControl.Visibility = Visibility.Visible;
        if (_controlPresenter is null) return;

        RootGrid.Children.Remove(_controlPresenter);
        _controlPresenter = null;
    }
}
