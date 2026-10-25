using Revit.Linter.ThemeManaging.Abstractions.Services;

namespace Revit.Linter.DiagnosticReportPresenter.Views;

public sealed partial class DiagnosticReportView
{
    /// <summary>
    /// Initializes a new diagnostic report view.
    /// </summary>
    /// <param name="themeService">The service that applies and updates the application theme.</param>
    public DiagnosticReportView(IThemeService themeService)
    {
        InitializeComponent();
        themeService.Register(this);
    }

    /// <summary>Initializes the view for the XAML designer, which has no application services.</summary>
    public DiagnosticReportView() => InitializeComponent();
}
