using Revit.Linter.ThemeManaging.Abstractions.Services;

namespace Revit.Linter.IgnoreListPresenter.Views;

/// <summary>
/// Displays the ignore list of the active document.
/// </summary>
public sealed partial class IgnoreListView
{
    /// <summary>
    /// Initializes the ignore-list view and registers it for application theme updates.
    /// </summary>
    /// <param name="themeService">Applies the current application theme to the view.</param>
    public IgnoreListView(IThemeService themeService)
    {
        InitializeComponent();
        themeService.Register(this);
    }
}
