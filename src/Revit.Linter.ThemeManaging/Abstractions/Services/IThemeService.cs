using System.Windows;

namespace Revit.Linter.ThemeManaging.Abstractions.Services;

public interface IThemeService
{
    void Register(FrameworkElement element);

    void ChangeTheme(bool isDarkTheme);
}
