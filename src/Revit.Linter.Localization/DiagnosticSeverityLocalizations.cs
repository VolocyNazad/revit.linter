namespace Revit.Linter.Localization;

/// <summary>
/// Provides localized labels for diagnostic severities.
/// </summary>
public static class DiagnosticSeverityLocalizations
{
    private const string ResourceBaseName = "Revit.Linter.Localization.DiagnosticSeverityLocalizations";

    /// <summary>
    /// Gets the localized label for a diagnostic severity.
    /// </summary>
    /// <param name="severity">The resource key identifying the severity.</param>
    /// <returns>The localized label, or <paramref name="severity"/> when no resource exists.</returns>
    public static string GetString(string severity)
        => LocalizationResourceReader.GetString(ResourceBaseName, severity);
}
