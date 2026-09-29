using Revit.Linter.ThemeManaging.Abstractions.Services;

namespace Revit.Linter.DiagnosticListPresenter.Views;

/// <summary>
/// Displays registered diagnostics and controls for filtering and configuring them.
/// </summary>
public sealed partial class DiagnosticListView
{
    /// <summary>
    /// Initializes the diagnostic-list view and registers it for application theme updates.
    /// </summary>
    /// <param name="themeService">Applies the current application theme to the view.</param>
    public DiagnosticListView(IThemeService themeService)
    {
        InitializeComponent();
        themeService.Register(this);
    }
}
