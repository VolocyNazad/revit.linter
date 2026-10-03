namespace Revit.Linter.Localization;

/// <summary>
/// Provides the localized texts shared by every help tooltip.
/// </summary>
public static class HelpToolTipLocalizations
{
    private const string ResourceBaseName = "Revit.Linter.Localization.HelpToolTipLocalizations";

    /// <summary>
    /// Gets a localized help tooltip text.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized text, or <paramref name="key"/> when no resource exists.</returns>
    public static string GetString(string key)
        => LocalizationResourceReader.GetString(ResourceBaseName, key);
}
