namespace Revit.Linter.ThemeManaging.Resources;

/// <summary>
/// Provides the palette and the control styles shared by every view of the add-in.
/// </summary>
/// <remarks>
/// Merge one instance into the resources of each root view instead of declaring colors or restyling
/// controls there. The dictionary starts with the light theme;
/// <see cref="Abstractions.Services.IThemeService"/> switches the theme of registered element trees
/// afterwards. Views refer to the dictionary by type rather than by a pack URI, so they keep resolving it
/// after ILRepack merges assemblies.
/// </remarks>
public sealed partial class SharedThemeResources
{
    /// <summary>
    /// Initializes the dictionary with the light palette and the shared styles.
    /// </summary>
    public SharedThemeResources() => InitializeComponent();

    /// <summary>
    /// Switches the dictionary between the light and the dark theme.
    /// </summary>
    /// <param name="isDarkTheme"><see langword="true"/> to apply the dark theme; otherwise, the light theme.</param>
    /// <remarks>
    /// Replaces the palette dictionary, so every value referenced through <c>DynamicResource</c> is updated
    /// in the elements that merged this instance. Applying the theme that is already in effect changes
    /// nothing.
    /// </remarks>
    public void ApplyTheme(bool isDarkTheme)
    {
        for (int index = 0; index < MergedDictionaries.Count; index++)
        {
            switch (MergedDictionaries[index])
            {
                case LightPalette when isDarkTheme:
                    MergedDictionaries[index] = new DarkPalette();
                    break;
                case DarkPalette when !isDarkTheme:
                    MergedDictionaries[index] = new LightPalette();
                    break;
            }
        }
    }
}
