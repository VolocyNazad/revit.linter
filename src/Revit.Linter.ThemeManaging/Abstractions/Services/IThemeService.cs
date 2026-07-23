using System.Windows;
using System.Windows.Media;

namespace Revit.Linter.ThemeManaging.Abstractions.Services;

public interface IThemeService
{
    void Register(FrameworkElement element);

    void ChangeTheme(bool isDarkTheme, Color backgroundColor);
}
