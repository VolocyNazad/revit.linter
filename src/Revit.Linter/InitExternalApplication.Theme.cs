// Theme propagation from Revit to the add-in views.

using Revit.Linter.ThemeManaging.Abstractions.Services;
#if BEFORE2024
using Revit.Sugar;
#endif

namespace Revit.Linter;

internal sealed partial class InitExternalApplication
{
#if !BEFORE2024
    private void InitializeThemeHandling()
    {
        ChangePluginTheme();
        Application.ThemeChanged += Application_ThemeChanged;
    }

    private static void Application_ThemeChanged(object? sender, Autodesk.Revit.UI.Events.ThemeChangedEventArgs e)
        => ChangePluginTheme();

    private static void ChangePluginTheme()
    {
        bool isDarkTheme = UIThemeManager.CurrentTheme == UITheme.Dark;
        GetService<IThemeService>().ChangeTheme(isDarkTheme);
    }
#endif
}

