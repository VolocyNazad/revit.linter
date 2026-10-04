using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using System.Windows;

namespace Revit.Linter.ThemeManaging.Resources;

/// <summary>
/// Provides the styles and theme resources shared by every view of the add-in.
/// </summary>
/// <remarks>
/// Merge one instance into the resources of each root view instead of declaring theme dictionaries there.
/// The dictionary starts with the light theme; <see cref="Abstractions.Services.IThemeService"/> switches the
/// base theme of registered element trees afterwards. The type is declared in code rather than as a XAML
/// resource dictionary so that views refer to it by type and keep resolving it after ILRepack merges assemblies.
/// </remarks>
public sealed class SharedThemeResources : ResourceDictionary
{
    private const string ControlDefaultsSource =
        "/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml";

    /// <summary>
    /// Initializes the dictionary with the light base theme, the add-in palette and the default control styles.
    /// </summary>
    public SharedThemeResources()
    {
        MergedDictionaries.Add(new BundledTheme
        {
            BaseTheme = BaseTheme.Light,
            PrimaryColor = PrimaryColor.Cyan,
            SecondaryColor = SecondaryColor.DeepOrange
        });
        MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(ControlDefaultsSource, UriKind.Relative)
        });
    }
}
