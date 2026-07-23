using Revit.Linter.FixReportPresenter.ViewModels;

using Revit.Linter.ThemeManaging.Abstractions.Services;

namespace Revit.Linter.FixReportPresenter.Views;

public sealed partial class FixReportView
{
    public FixReportView(IServiceProvider serviceProvider, IThemeService themeService)
    {
        ServiceLocator.Initialize(serviceProvider);
        InitializeComponent();
        themeService.Register(this);
    }
}
