namespace Revit.Linter.Localization;

public static class DiagnosticSeverityLocalizations
{
    private const string ResourceBaseName = "Revit.Linter.Localization.DiagnosticSeverityLocalizations";

    public static string GetString(string severity)
        => LocalizationResourceReader.GetString(ResourceBaseName, severity);
}
