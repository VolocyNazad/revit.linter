using Revit.Linter.ThemeManaging.Abstractions.Services;

namespace Revit.Linter.FixReportPresenter.Views;

/// <summary>
/// Displays fix reports with document filtering and element-link interaction.
/// </summary>
public sealed partial class FixReportView
{
    /// <summary>
    /// Initializes the fix-report view and registers it for application theme updates.
    /// </summary>
    /// <param name="themeService">Applies the current application theme to the view.</param>
    public FixReportView(IThemeService themeService)
    {
        InitializeComponent();
        themeService.Register(this);
    }

    /// <summary>Initializes the view for the XAML designer, which has no application services.</summary>
    public FixReportView() => InitializeComponent();
}
