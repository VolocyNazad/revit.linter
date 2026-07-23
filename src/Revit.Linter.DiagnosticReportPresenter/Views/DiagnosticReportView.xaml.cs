using Revit.Linter.DiagnosticReportPresenter.ViewModels;

using Revit.Linter.ThemeManaging.Abstractions.Services;

namespace Revit.Linter.DiagnosticReportPresenter.Views;

public sealed partial class DiagnosticReportView
{
    public DiagnosticReportView(IServiceProvider serviceProvider, IThemeService themeService)
    {
        ServiceLocator.Initialize(serviceProvider);
        InitializeComponent();
        themeService.Register(this);
    }
}
