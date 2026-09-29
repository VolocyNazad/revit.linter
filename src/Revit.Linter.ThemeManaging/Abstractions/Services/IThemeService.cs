using System.Windows;

namespace Revit.Linter.ThemeManaging.Abstractions.Services;

/// <summary>
/// Applies and updates the application theme for registered WPF element trees.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Registers an element tree and immediately applies the current theme to it.
    /// </summary>
    /// <param name="element">The root element to register using a weak reference.</param>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
    void Register(FrameworkElement element);

    /// <summary>
    /// Changes the current theme and applies it to all registered element trees that are still alive.
    /// </summary>
    /// <param name="isDarkTheme"><see langword="true"/> to apply the dark theme; otherwise, the light theme.</param>
    void ChangeTheme(bool isDarkTheme);
}
