namespace Revit.Linter.Localization;

/// <summary>
/// Provides localized labels for diagnostic report filters.
/// </summary>
public static class DiagnosticReportFilterLocalizations
{
    private const string ResourceBaseName = "Revit.Linter.Localization.DiagnosticReportFilterLocalizations";

    /// <summary>
    /// Gets the localized label for a diagnostic report filter.
    /// </summary>
    /// <param name="key">The resource key identifying the filter.</param>
    /// <returns>The localized label, or <paramref name="key"/> when no resource exists.</returns>
    public static string GetString(string key)
        => LocalizationResourceReader.GetString(ResourceBaseName, key);
}
