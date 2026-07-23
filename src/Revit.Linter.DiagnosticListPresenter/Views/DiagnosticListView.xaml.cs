using Revit.Linter.DiagnosticListPresenter.ViewModels;

using Revit.Linter.ThemeManaging.Abstractions.Services;

namespace Revit.Linter.DiagnosticListPresenter.Views;

public sealed partial class DiagnosticListView
{
    public DiagnosticListView(IServiceProvider serviceProvider, IThemeService themeService)
    {
        ServiceLocator.Initialize(serviceProvider);
        InitializeComponent();
        themeService.Register(this);
    }
}
