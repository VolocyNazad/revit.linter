using Revit.Linter.ThemeManaging.Abstractions.Services;

namespace Revit.Linter.DiagnosticListPresenter.Views;

public sealed partial class DiagnosticListView
{
    public DiagnosticListView(IThemeService themeService)
    {
        InitializeComponent();
        themeService.Register(this);
    }
}
