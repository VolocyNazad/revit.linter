namespace Revit.Linter.Localization;

/// <summary>
/// Provides localized labels for diagnostic target types.
/// </summary>
public static class DiagnosticTargetTypeLocalizations
{
    private const string ResourceBaseName = "Revit.Linter.Localization.DiagnosticTargetTypeLocalizations";

    /// <summary>
    /// Gets the localized label for a diagnostic target type.
    /// </summary>
    /// <param name="targetType">The resource key identifying the target type.</param>
    /// <returns>The localized label, or <paramref name="targetType"/> when no resource exists.</returns>
    public static string GetString(string targetType)
        => LocalizationResourceReader.GetString(ResourceBaseName, targetType);
}
