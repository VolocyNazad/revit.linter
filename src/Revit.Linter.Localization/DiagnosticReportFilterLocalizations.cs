namespace Revit.Linter.Localization;

public static class DiagnosticReportFilterLocalizations
{
    private const string ResourceBaseName = "Revit.Linter.Localization.DiagnosticReportFilterLocalizations";

    public static string GetString(string key)
        => LocalizationResourceReader.GetString(ResourceBaseName, key);
}
